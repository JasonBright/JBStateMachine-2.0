using System;
using System.Collections.Generic;

namespace JasonBright.StateMachine
{
    public class StateConfiguration<TState, TTrigger> : IStateConfiguration<TState, TTrigger>
    {
        public StateMachine<TState, TTrigger> machine { get; private set; }
        public IStateRepresentation<TState, TTrigger> stateRepresentation { get; private set; }

        public TState state
        {
            get
            {
                return stateRepresentation.state;
            }
        }
        

        public StateConfiguration(StateMachine<TState, TTrigger> machine, TState state, IStateController controller)
        {
            this.machine = machine;
            stateRepresentation = new StateRepresentation<TState, TTrigger>(state, controller);
        }

        public IStateConfiguration<TState, TTrigger> Permit(TTrigger trigger, TState state)
        {
            stateRepresentation.AddTransition(trigger, state, null);
            return this;
        }

        public IStateConfiguration<TState, TTrigger> PermitIf(TTrigger trigger, TState state, Func<bool> guard)
        {
            stateRepresentation.AddTransition(trigger, state, guard);
            return this;
        }

        public IStateConfiguration<TState, TTrigger> PermitReentry(TTrigger trigger)
        {
            Permit(trigger, state);
            return this;
        }

        public IStateConfiguration<TState, TTrigger> SetAsAnyState()
        {
            machine.SetAnyState(stateRepresentation);
            return this;
        }

        public IStateConfiguration<TState, TTrigger> PermitReentryIf(TTrigger trigger, Func<bool> guard)
        {
            if (guard() == true)
            {
                PermitReentry(trigger);
            }

            return this;
        }

        public IStateConfiguration<TState, TTrigger> OnEnter(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action", "action parameter must not be null");
            }

            stateRepresentation.AddEntryAction(action);
            return this;
        }

        public IStateConfiguration<TState, TTrigger> OnGetStateData(StateDataDelegate<ITransition<TState, TTrigger>> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action", "action parameter must not be null");
            }
            stateRepresentation.SetEntryStateData(action);
            return this;
        }

        public IStateConfiguration<TState, TTrigger> OnExit(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action", "action parameter must not be null");
            }

            stateRepresentation.AddExitAction(action);
            return this;
        }

        public IStateConfiguration<TState, TTrigger> OnExitData(Action<ITransition<TState, TTrigger>> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action", "action parameter must not be null");
            }

            stateRepresentation.SetExitData(action);
            return this;
        }

        public IStateConfiguration<TState, TTrigger> SubstateOf(TState superState)
        {
            // Check for accidental identical cyclic configuration.
            if (state.Equals(superState))
            {
                throw new ArgumentException("Configuring " + state + " as a substate of " + superState + " creates an illegal cyclic configuration.");
            }

            var superStateRepresentation = machine.GetStateRepresentation(superState);
            if (superStateRepresentation.superState != null)
            {
                throw new Exception(
                    "Only single-tier adoption supported. Are you sure that you really need it? Maybe better to make another one state machine");
            }
            
            stateRepresentation.superState = superStateRepresentation;
            return this;
        }
    }
}
