namespace Electra.Common.Decorators
{
    // public sealed class TimingCommandDecorator<T, TReturn> : DecoratorBaseAsync<T>, ICommandAsync<T>
    // {
    //     readonly Stopwatch sw = new Stopwatch();
    //     
    //     public TimingCommandDecorator(ICommandAsync<T, TReturn> cmd, ILogger log) : base(cmd, log)
    //     {
    //     }
    //     
    //     public override async Task<TReturn> ExecuteAsync(ICommandParameter parameter)
    //     {
    //         var path = $"{cmd.GetType()}.{nameof(ExecuteAsync)}()";
    //         sw.Start();
    //         var result = await cmd.ExecuteAsync(parameter);
    //         sw.Stop();
    //         log.LogInformation($"invoking {path} took {sw.ElapsedMilliseconds}");
    //         return result;
    //     }
    // }
}