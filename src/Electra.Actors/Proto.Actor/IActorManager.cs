using Proto;

namespace Electra.Actors
{
    public interface IActorManager<T> where T : IActor
    {
        Task RequestAsync<TMessage>(TMessage message) where TMessage : IActorMessage;
        Task<TResult> RequestAsync<TMessage, TResult>(TMessage message) where TMessage : IActorMessage;
        Task SendAsync<TMessage>(TMessage message) where TMessage : IActorMessage;
    }
}