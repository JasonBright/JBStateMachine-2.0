namespace JasonBright.StateMachine
{
    public class Transition<TState, TTrigger> : ITransition<TState, TTrigger>
    {
        public TState source { get; private set; }
        public TState destination { get; private set; }
        public TTrigger trigger { get; private set; }

        /// <summary>
        /// Construct a transition.
        /// </summary>
        /// <param name="source">The state transitioned from.</param>
        /// <param name="destination">The state transitioned to.</param>
        /// <param name="trigger">The trigger that caused the transition.</param>
        public Transition(TState source, TState destination, TTrigger trigger)
        {
            this.source = source;
            this.destination = destination;
            this.trigger = trigger;
        }

        public bool isReentry
        {
            get
            {
                return source.Equals(destination);
            }
        }

        public EnterDataBase enterDataBase { get; set; }
        public ExitDataBase ExitData { get; set; }
    }
}
