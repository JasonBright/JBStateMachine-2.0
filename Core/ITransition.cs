namespace JasonBright.StateMachine
{
    public interface ITransition<TState, TTrigger>
    {
        /// <summary>
        /// The state transitioned from.
        /// </summary>
        TState source { get; }

        /// <summary>
        /// The state transitioned to.
        /// </summary>
        TState destination { get; }

        /// <summary>
        /// The trigger that caused the transition.
        /// </summary>
        TTrigger trigger { get; }

        /// <summary>
        /// True if the transition is a re-entry, i.e. the identity transition.
        /// </summary>
        bool isReentry { get; }
        
        EnterDataBase enterDataBase { get; set; }
        ExitDataBase ExitData { get; set; }
    }
}
