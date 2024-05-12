using Proto;
using ILogger = Serilog.ILogger;

namespace Electra.Actors.Proto.Actor
{
    // https://github.com/AsynkronIT/protoactor-dotnet/tree/dev/examples/DependencyInjection
    // todo - consider refactoring ActorManager to use three generic parameters so it can be injected
    // todo - consider creating a non-generic version of ActorManager (examples demonstrate using casting to check messages)
    public class ActorManager<T> : IActorManager<T> where T : IActor
    {
        readonly RootContext context = new(new ActorSystem());
        readonly IActorFactory factory;  // todo - proto var is never used - figure out if we need this
        readonly ILogger log;
        readonly PID pid;

        public ActorManager(IActorFactory factory, ILogger log)
        {
            this.log = log;
            this.factory = factory;
            if(factory == null)
                throw new ArgumentNullException(nameof(factory));
            log.Information($"Instantiating ActorManager for {typeof(T)}");
            pid = this.factory.GetActor<T>();
            //EventStream.Instance.Subscribe<TriviaMessage>(x => log.Information($"EventStream reply: {x}"));
            log.Information($"finished creating ActorManager<{typeof(T)}>");
        }

        public RootContext GetRootContext() => context;

        public PID GetPID() => pid;

        // todo - actorManager - remove async anti-pattern Task.Run(() =>) and Task.Delay(0)
        public async Task RequestAsync<TMessage>(TMessage message)
            where TMessage : IActorMessage =>
            await Task.Run(() => context.Request(pid, message));

        public async Task<TResult> RequestAsync<TMessage, TResult>(TMessage message)
            where TMessage : IActorMessage
        {
            var result = await context.RequestAsync<TResult>(pid, message);
            log.Debug($"{nameof(RequestAsync)} returned with {result} value");
            return result;
        }

        public async Task SendAsync<TMessage>(TMessage message)
            where TMessage : IActorMessage
        {
            await Task.Delay(0);
            context.Send(pid, message);
        }
    }
}