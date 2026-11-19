using System;
using FreightQuoting.Core.Models;
using FreightQuoting.Core.Plugins;
using FreightQuoting.Core.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using OpenAI.Chat;
namespace FreightQuoting.Core.Services;

public interface IQuoteOrchestratorService
{
    Task<QuoteChatResponse> ProcessCustomerMessageAsync(ChatHistory history, string userMessage);
}

public class QuoteOrchestratorService : IQuoteOrchestratorService
{
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _deployment;

    public QuoteOrchestratorService(string endpoint, string apiKey, string deployment)
    {
        _endpoint = endpoint;
        _apiKey = apiKey;
        _deployment = deployment;
    }

    public async Task<QuoteChatResponse> ProcessCustomerMessageAsync(ChatHistory history, string userMessage)
    {
       // Fresh per-request telemetry context
        var telemetry = new QuoteTelemetryContext();

        var builder = Kernel.CreateBuilder();
        builder.AddAzureOpenAIChatCompletion(_deployment, _endpoint, _apiKey);
        builder.Services.AddSingleton(telemetry);
        builder.Services.AddSingleton<IFunctionInvocationFilter, ToolObservabilityFilter>();
        builder.Plugins.AddFromType<FreightLogisticsPlugin>("FreightOps");

        var kernel = builder.Build();
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        history.AddUserMessage(userMessage);
        telemetry.LogStep("User_Message_Received");

        var settings = new AzureOpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            Temperature = 0.0
        };

        var response = await chatService.GetChatMessageContentAsync(history, settings, kernel);
        history.AddAssistantMessage(response.Content ?? string.Empty);

        if (response.InnerContent is ChatCompletion raw && raw.Usage is not null)
        {
            telemetry.PromptTokens += raw.Usage.InputTokenCount;
            telemetry.CompletionTokens += raw.Usage.OutputTokenCount;
        }

        return new QuoteChatResponse(
            response.Content ?? "No response generated.",
            telemetry.ExecutionPath,
            telemetry.PromptTokens,
            telemetry.CompletionTokens,
            telemetry.TotalCostUsd
        );
    }
}