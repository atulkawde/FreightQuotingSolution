using System;
using Microsoft.SemanticKernel;
namespace FreightQuoting.Core.Telemetry;

public class ToolObservabilityFilter(QuoteTelemetryContext telemetry) : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        telemetry.LogStep($"Plugin_Invoked:{context.Function.PluginName}.{context.Function.Name}");
        await next(context);
    }
}

