using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aero.Common.FP;

public class Pipeline<TInput, TError, TOutput>
{
    private readonly List<Func<TInput, Task<Result<TError, TOutput>>>> _steps = new();

    public Pipeline<TInput, TError, TOutput> AddStep(Func<TInput, Task<Result<TError, TOutput>>> step)
    {
        _steps.Add(step);
        return this;
    }

    public async Task<Result<TError, TOutput>> Execute(TInput input)
    {
        Result<TError, TOutput> current = new Result<TError, TOutput>.Ok(default!);

        foreach (var step in _steps)
        {
            current = await step(input);
            if (current is Result<TError, TOutput>.Failure)
                return current;
        }

        return current;
    }
}
