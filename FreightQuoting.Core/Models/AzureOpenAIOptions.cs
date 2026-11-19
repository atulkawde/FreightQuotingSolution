using System;

namespace FreightQuoting.Core.Models;

public class AzureOpenAIOptions
{
    public const string SectionName = "AzureOpenAI";
    public required string Endpoint { get; set; }
    public required string ApiKey { get; set; }
    public required string Deployment { get; set; }
}
