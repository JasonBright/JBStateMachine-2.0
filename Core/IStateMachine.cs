using System;
using System.Collections.Generic;

namespace JasonBright.StateMachine
{
    public interface IStateMachine<TState, TTrigger> : IDisposable
    {
        TState currentState { get; }
        ICollection<TTrigger> permittedTriggers { get; }

        /// <summary>
        /// Begin configuration of the entry/exit actions and allowed
        /// transitions when the state machine is in a particular state.
        /// </summary>
        /// <param name="state">The state to configure.</param>
        /// <returns>A configuration object through which the state can be
        /// configured.</returns>
        IStateConfiguration<TState, TTrigger> Configure(TState state, IStateController controller);

        void Start(TState initState);

        void SetAnyState(IStateRepresentation<TState, TTrigger> stateRepresentation);

        /// <summary>
        /// Transition from the current state via the specified trigger.
        /// The target state is determined by the configuration of the current
        /// state.
        /// Actions associated with leaving the current state and entering the
        /// new one will be invoked.
        /// </summary>
        /// <param name="trigger">The trigger to fire.</param>
        /// <exception cref="System.InvalidOperationException">The current state
        /// does not allow the trigger to be fired.</exception>
        void Fire(TTrigger trigger);

        /// <summary>
        /// Determine if the state machine is in the supplied state.
        /// </summary>
        /// <param name="state">The state to test for.</param>
        /// <returns>True if the current state is equal to, or a substate of,
        /// the supplied state.</returns>
        bool IsInState(TState state);

        /// <summary>
        /// Returns true if <paramref name="trigger"/> can be fired in the
        /// current state.
        /// </summary>
        /// <param name="trigger">Trigger to test.</param>
        /// <returns>True if the trigger can be fired, false
        /// otherwise.</returns>
        bool CanFire(TTrigger trigger);

        /// <summary>
        /// Registers a callback that will be invoked every time the
        /// statemachine transitions from one state into another.
        /// </summary>
        /// <param name="onTransitionAction">The action to execute, accepting
        /// the details of the transition.</param>
        void OnTransitioned(Action action);

        /// <summary>
        /// Return the state representation object for the given state.
        /// </summary>
        /// <param name="state">The state to find the representation object
        /// for.</param>
        IStateRepresentation<TState, TTrigger> GetStateRepresentation(TState state);

        /// <summary>
        /// A human-readable representation of the state machine.
        /// </summary>
        /// <returns>A description of the current state and permitted
        /// triggers.</returns>
        string ToString();
    }
}
