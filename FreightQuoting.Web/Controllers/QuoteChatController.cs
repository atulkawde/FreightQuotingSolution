using System.Text.Json;
using FreightQuoting.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FreightQuoting.Web.Controllers
{
    public record ChatRequestDto(string Message);
    public class QuoteChatController(IQuoteOrchestratorService orchestrator) : Controller
    {
    private const string SessionKey = "QuoteChatHistory";

    private const string SystemPrompt = """
        You are an Enterprise Freight Quoting Agent. Follow this exact sequence:
        1. Call validate_locations first. If it returns HALTED, stop and politely ask the customer for the missing origin or destination city.
        2. Call calculate_compliance_surcharge based on cargo sensitivity.
        3. Call get_contract_rate. If it fails or throws an exception, automatically recover by calling get_spot_market_fallback_rate.
        4. Sum the base rate and surcharges accurately and return a clean final quote summary.
        """;

    public IActionResult Index() => View();

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequestDto request)
    {
        var history = GetOrCreateHistory();
        var result = await orchestrator.ProcessCustomerMessageAsync(history, request.Message);
        SaveHistory(history);

        return Json(result);
    }
    [HttpPost]
    public IActionResult ResetSession()
    {
        HttpContext.Session.Remove(SessionKey);
        return Ok();
    }

    private ChatHistory GetOrCreateHistory()
    {
        var json = HttpContext.Session.GetString(SessionKey);
        if (!string.IsNullOrEmpty(json))
        {
            var saved = JsonSerializer.Deserialize<ChatHistory>(json);
            if (saved is not null) return saved;
        }
        return new ChatHistory(SystemPrompt);
    }

    private void SaveHistory(ChatHistory history) =>
        HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(history));
    }

    
}
