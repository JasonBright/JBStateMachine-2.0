using System;
using System.Collections.Generic;
using System.Threading;

namespace JasonBright.StateMachine
{
    public delegate EnterDataBase StateDataDelegate<in T>(T transition);
    
    public class StateRepresentation<TState, TTrigger> : IStateRepresentation<TState, TTrigger>
    {
        // Remember that transitions are always outgoing for a state. They
        // can never be incoming. Every state can only control where it can
        // go next, not from where the state was reached.

        // These transitions belong to this state only, not to the super states.

        private readonly IDictionary<TTrigger, List< TransitionState<TState> > > _ownTransitions = new Dictionary<TTrigger, List< TransitionState<TState>>>();
        private bool _isActive;
        private readonly IList<Action> _entryActions = new List<Action>();
        private readonly IList<Action> _exitActions = new List<Action>();
        
        private StateDataDelegate<ITransition<TState, TTrigger>> entryDataAction;
        private Action<ITransition<TState, TTrigger>> exitDataAction;
        
        private readonly IList<IStateRepresentation<TState, TTrigger>> _subStates = new List<IStateRepresentation<TState, TTrigger>>();

        public TState state { get; private set; }
        public IStateRepresentation<TState, TTrigger> superState { get; set; }
        public IStateController Controller { get; private set; }

        private CancellationTokenSource exitStateCancellation;

        // These are all the transitions i.e.
        // inherited (from super states) + own transitions.
        public IDictionary<TTrigger, List< TransitionState<TState> > > transitions
        {
            get
            {
                return _ownTransitions;
                /*IDictionary<TTrigger, TState> allTransitions = new Dictionary<TTrigger, TState>();

                if (superState != null)
                {
                    foreach (KeyValuePair<TTrigger, TState> item in superState.transitions)
                    {
                        if (allTransitions.ContainsKey(item.Key))
                        {
                            throw new InvalidOperationException("The trigger '" + item.Key + "' is already present in one of the super state transitions of the state '" + item.Value + "'.");
                        }

                        allTransitions.Add(item);
                    }
                }

                foreach (KeyValuePair<TTrigger, TransitionState> item in _ownTransitions)
                {
                    if (allTransitions.ContainsKey(item.Key))
                    {
                        throw new InvalidOperationException("The trigger '" + item.Key + "' is already present in one of the super state transitions of the state '" + item.Value + "'.");
                    }

                    allTransitions.Add(item);
                }

                return allTransitions;*/
            }
        }

        public ICollection<TTrigger> permittedTriggers
        {
            get 
            {
                return transitions.Keys;
            }
        }

        public StateRepresentation(TState state, IStateController controller)
        {
            this.state = state;
            Controller = controller;
        }

        public bool CanHandle(TTrigger trigger)
        {
            return transitions.ContainsKey(trigger);
        }

        public void AddTransition(TTrigger trigger, TState state, Func<bool> condition)
        {
            /*if (_ownTransitions.ContainsKey(trigger))
            {
                throw new InvalidOperationException("The trigger '" + trigger + "' is already present in one of the transitions of the state '" + state + "'.");
            }*/
            if (_ownTransitions.ContainsKey(trigger) == false)
            {
                _ownTransitions.Add(trigger, new List<TransitionState<TState>>());
            }

            _ownTransitions[ trigger ].Add(new TransitionState<TState>(state, condition));
        }

        public TransitionState<TState> GetTransitionState(TTrigger trigger)
        {
            if (!transitions.ContainsKey(trigger))
            {
                throw new KeyNotFoundException("No transition present for trigger " + trigger);
            }

            var availableTransitions = transitions[ trigger ];
            foreach (var availableTransition in availableTransitions)
            {
                if (availableTransition.Condition == null)
                    return availableTransition;
                
                if (availableTransition.Condition.Invoke())
                    return availableTransition;
            }

            return null;
        }

        public void OnEnter(ITransition<TState, TTrigger> transition)
        {
            // In order to enter this state we have to enter its super states
            // first.
            if (superState != null)
            {
                superState.OnEnter(transition);
            }

            if (_isActive)
            {
                return;
            }
            
            _isActive = true;
            
            foreach (Action action in _entryActions)
            {
                action();
            }
            
            InjectAsyncStateController();
            
            var data = entryDataAction?.Invoke(transition);
            transition.enterDataBase = data;

            Controller?.OnEntered(data);
        }

        private void InjectAsyncStateController()
        {
            if (Controller is IAsyncStateController asyncStateController)
            {
                exitStateCancellation = new CancellationTokenSource();
                asyncStateController.Token = exitStateCancellation.Token;
                asyncStateController.IsActive = true;
            }
        }

        private void DisposeAsyncStateController()
        {
            if (Controller is IAsyncStateController asyncStateController)
            {
                if (exitStateCancellation != null)
                {
                    exitStateCancellation.Cancel();
                    exitStateCancellation.Dispose();
                    exitStateCancellation = null;
                }
                
                asyncStateController.IsActive = false;
            }
        }
        
        public void OnExit(ITransition<TState, TTrigger> transition, bool forced = false)
        {
            // 1. If this state is inactive then we are not inside this state or
            // any of its sub-states.
            // 2. Don't call exit actions if this state or any of its sub-states
            // are the destination state.
            if (forced == false && (  !_isActive || Includes(transition.destination) )) //todo: рефакторнуть бы эту дичь
            {
                return;
            }

            // Call the exit actions.
            foreach (Action action in _exitActions)
            {
                action();
            }
            
            _isActive = false;
            DisposeAsyncStateController();

            var data = Controller?.OnExited();
            transition.ExitData = data;
            
            exitDataAction?.Invoke(transition);
            
            // Exit all super states recursively.
            if (superState != null)
            {
                superState.OnExit(transition);
            }
        }
        
        public void AddEntryAction(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action", "Action parameter must not be null");
            }

            if (_entryActions.Contains(action))
            {
                throw new NotSupportedException("Action " + action + " is already added to entryActions");
            }

            _entryActions.Add(action);
        }
        
        public void SetEntryStateData(StateDataDelegate<ITransition<TState, TTrigger>> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action", "Action parameter must not be null");
            }

            entryDataAction = action;
        }

        public void AddExitAction(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action", "Action parameter must not be null");
            }

            if (_exitActions.Contains(action))
            {
                throw new NotSupportedException("Action " + action + " is already added to exitActions");
            }

            _exitActions.Add(action);
        }
        
        public void SetExitData(Action<ITransition<TState, TTrigger>> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action", "Action parameter must not be null");
            }

            exitDataAction = action;
        }

        public bool Includes(TState state)
        {
            bool includesState = false;

            if (this.state.Equals(state))
            {
                includesState = true;
            }
            else
            {
                foreach (IStateRepresentation<TState, TTrigger> subState in _subStates)
                {
                    if (subState.Includes(state))
                    {
                        includesState = true;
                        break;
                    }
                }
            }

            return includesState;
        }
    }
}
