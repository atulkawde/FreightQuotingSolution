using System;

namespace FreightQuoting.Core.Models;

public record QuoteChatResponse(
    string ReplyMessage,
    List<string> ExecutionPath,
    int PromptTokens,
    int CompletionTokens,
    decimal TotalCostUsd);