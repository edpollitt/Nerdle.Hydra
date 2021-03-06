using System;
using System.Threading.Tasks;

namespace Nerdle.Hydra
{
    public interface IFailable<out TComponent> : IFailable
    {
        void Execute(Action<TComponent> command);
        TResult Execute<TResult>(Func<TComponent, TResult> query);
        Task ExecuteAsync(Func<TComponent, Task> command);
        Task<TResult> ExecuteAsync<TResult>(Func<TComponent, Task<TResult>> query);
    }

    public interface IFailable
    {
        string ComponentId { get; }
        bool IsAvailable { get; }

        event EventHandler<Exception> Failed;
        event EventHandler Recovered;
    }
}