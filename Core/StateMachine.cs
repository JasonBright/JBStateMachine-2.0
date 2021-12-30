using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JasonBright.StateMachine
{
    public partial class StateMachine<TState, TTrigger> : IStateMachine<TState, TTrigger>
    {
        private readonly IDictionary<TState, IStateConfiguration<TState, TTrigger>> _stateConfigurations = new Dictionary<TState, IStateConfiguration<TState, TTrigger>>();
        private Action _onTransition;
        
        private readonly Queue<TTrigger> fireQueue = new Queue<TTrigger>();

        private string name;

        public TState currentState { get; private set; }

        public ICollection<TTrigger> permittedTriggers
        {
            get
            {
                var result = currentStateRepresentation.permittedTriggers;
                if (anyState != null)
                {
                    result = result.Union(anyState.permittedTriggers).ToList();
                }

                var superState = currentStateRepresentation.superState;
                if (superState != null)
                {
                    result = result.Union(superState.permittedTriggers).ToList();
                }
                return result;
            }
        }

        private IStateRepresentation<TState, TTrigger> currentStateRepresentation
        {
            get
            {
                return GetStateRepresentation(currentState);
            }
        }

        private IStateRepresentation<TState, TTrigger> anyState;
        
        [Obsolete("Инициализация стартового состояния устарела и не используется. Осталось только для совместимости. Использовать Start")]
        public StateMachine(TState initialState)
        {
            
        }

        public StateMachine(string name = "")
        {
            this.name = name;
        }

        public IStateConfiguration<TState, TTrigger> Configure(TState state, IStateController controller)
        {
            if (_stateConfigurations.ContainsKey(state))
            {
                return _stateConfigurations[state];
            }
            else
            {
                IStateConfiguration<TState, TTrigger> configuration = new StateConfiguration<TState, TTrigger>(this, state, controller);
                _stateConfigurations.Add(state, configuration);
                return configuration;
            }
        }

        public void Start(TState initState)
        {
            IStateRepresentation<TState, TTrigger> newStateRepresentation = GetStateRepresentation(initState);

            ITransition<TState, TTrigger> transition = new Transition<TState, TTrigger>(initState, initState, default);
            currentState = initState;
            Debug.Log($"State Machine ({name} Started {currentState}");
            newStateRepresentation.OnEnter(transition);
        }

        public void SetAnyState(IStateRepresentation<TState, TTrigger> stateRepresentation)
        {
            if (anyState != null)
            {
                throw new Exception($"AnyState already set: {anyState.state}");
            }
            anyState = stateRepresentation;
        }

        public void Fire(TTrigger trigger)
        {
            if (fireQueue.Count > 0)
            {
                fireQueue.Enqueue(trigger);
                return;
            }
            
            fireQueue.Enqueue(trigger);


            FireTrigger(trigger);
            fireQueue.Dequeue();
            while (fireQueue.Count > 0)
            {
                FireTrigger(fireQueue.Peek());
                fireQueue.Dequeue();
            }
        }

        private void FireTrigger(TTrigger trigger)
        {
            if (!permittedTriggers.Contains(trigger))
            {
                //throw new NotSupportedException("'" + trigger + "' trigger is not configured for '" + currentState + "' state.");
                Debug.Log(name + " '" + trigger + "' trigger is not configured for '" + currentState + "' state.");
                return;
            }
            
            TState oldState = currentState;
            TransitionState<TState> newTransitionState = null;
            try
            { 
                newTransitionState = currentStateRepresentation.GetTransitionState(trigger);
            }
            catch
            {
                if (currentStateRepresentation.superState != null)
                {
                    try
                    {
                        newTransitionState = currentStateRepresentation.superState.GetTransitionState(trigger);
                    }
                    catch
                    {
                        //nothing
                    }
                }

                //попытка взять транзишин из AnyState. 
                //Её имеет смысл держать здесь, поскольку AnyState имеет приоритет ниже, чем переход стейта
                //и используется только если в стейте нет указанного триггера
                if (newTransitionState == null && anyState != null)
                {
                    newTransitionState = anyState.GetTransitionState(trigger);
                }
            }

            if (newTransitionState == null)
            {
                return;
            }

            TState newState = newTransitionState.State;
            ChangeState(oldState, newState, trigger);
        }

        private void ChangeState(TState oldState, TState newState, TTrigger trigger)
        {
            IStateRepresentation<TState, TTrigger> oldStateRepresentation = GetStateRepresentation(oldState);
            IStateRepresentation<TState, TTrigger> newStateRepresentation = GetStateRepresentation(newState);

            if (_onTransition != null)
            {
                _onTransition();
            }

            ITransition<TState, TTrigger> transition = new Transition<TState, TTrigger>(oldState, newState, trigger);
            oldStateRepresentation.OnExit(transition);
            newStateRepresentation.OnEnter(transition);
            currentState = newState;
            Debug.Log($"SM {name}; CURRENT STATE {currentState}");
        }

        public bool IsInState(TState state)
        {
            var currentStateRepresentation = GetStateRepresentation(currentState);
            return currentState.Equals(state) || (currentStateRepresentation.superState != null && currentStateRepresentation.superState.state.Equals(state));
        }

        public bool CanFire(TTrigger trigger)
        {
            return currentStateRepresentation.CanHandle(trigger);
        }

        public void OnTransitioned(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action", "Action parameter must not be null.");
            }

            _onTransition = action;
        }

        public override string ToString()
        {
            string triggers = string.Empty;

            foreach (TTrigger trigger in permittedTriggers)
            {
                triggers += trigger + ", ";
            }

            return (name + " Current state: " + currentState + " | Permitted triggers: " + triggers);
        }

        public IStateRepresentation<TState, TTrigger> GetStateRepresentation(TState state)
        {
            if (!_stateConfigurations.ContainsKey(state))
            {
                throw new NotSupportedException("State " + state + " is not configured yet so no representation exists for it!");
            }

            return _stateConfigurations[state].stateRepresentation;
        }

        public void Dispose()
        {
            IStateRepresentation<TState, TTrigger> oldStateRepresentation = GetStateRepresentation(currentState);
            
            ITransition<TState, TTrigger> transition = new Transition<TState, TTrigger>(currentState, currentState, default);
            oldStateRepresentation.OnExit(transition, true);
        }
    }
}
