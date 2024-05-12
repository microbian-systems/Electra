using System.Threading.Tasks;
using FluentValidation;
using Electra.Common.Commands;
using Microsoft.Extensions.Logging;

namespace Electra.Validators
{
    public class ValidationCommandHandlerDecorator<TCommand> : IAsyncCommand<TCommand>
    {
        private readonly ILogger log;
        private readonly IAsyncCommand<TCommand> handler;
        private readonly IValidator<TCommand> validator;

        public ValidationCommandHandlerDecorator(IValidator<TCommand> validator, IAsyncCommand<TCommand> handler, ILogger log) {
            this.log = log;
            this.handler  = handler;
            this.validator = validator;
        }

        // todo - investigate the following url for return async void as I'm doing here
        // https://msdn.microsoft.com/en-us/magazine/jj991977.aspx
        public async Task ExecuteAsync(TCommand command) 
        {
            log.LogInformation($"entered {nameof(ValidationCommandHandlerDecorator<TCommand>)}");
            var res = await validator.ValidateAsync(command);
            if(!res.IsValid)
                throw new ValidationException($"validation exception has occurred for {nameof(command)}", res.Errors);
            await handler.ExecuteAsync(command);
        }
    }
    
    public class ValidationCommandHandlerDecorator<TCommand, TResult> : IAsyncCommand<TCommand, TResult> 
    {
        private readonly ILogger log;
        private readonly IAsyncCommand<TCommand, TResult> handler;
        private readonly IValidator<TCommand> validator;

        public ValidationCommandHandlerDecorator(IValidator<TCommand> validator, IAsyncCommand<TCommand, TResult> handler, ILogger log) {
            this.log = log;
            this.handler = handler;
            this.validator = validator;
        }

        // todo - investigate the following url for return async void as I'm doing here
        // https://msdn.microsoft.com/en-us/magazine/jj991977.aspx
        public async Task<TResult> ExecuteAsync(TCommand command) 
        {
            log.LogInformation($"entered {nameof(ValidationCommandHandlerDecorator<TCommand, TResult>)}");
            var res = await validator.ValidateAsync(command);
            if(!res.IsValid)
                throw new ValidationException($"validation exception has occurred for {nameof(command)}", res.Errors);
            log.LogInformation($"validation succeeded in validation decorator for {typeof(TCommand)}");
            var results = await handler.ExecuteAsync(command);
            return results;
        }
    }
}