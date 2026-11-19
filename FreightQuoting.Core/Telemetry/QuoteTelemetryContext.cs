using System;

namespace FreightQuoting.Core.Telemetry;

public class QuoteTelemetryContext
{
    public List<string> ExecutionPath { get; } = new();
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public bool NeedsClarification { get; set; }
    public string? ClarificationMessage { get; set; }

    public decimal TotalCostUsd => 
        Math.Round((PromptTokens * 0.00000015m) + (CompletionTokens * 0.00000060m), 6);

    public void LogStep(string step) => ExecutionPath.Add(step);

}
