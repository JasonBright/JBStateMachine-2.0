using System.Threading;

namespace JasonBright.StateMachine
{
    public interface IAsyncStateController
    {
        public CancellationToken Token { get; set; }
        public bool IsActive { get; set; }
    }
}