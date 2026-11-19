using System;
using System.ComponentModel;
using System.Text.Json;
using FreightQuoting.Core.Telemetry;
using Microsoft.SemanticKernel;

namespace FreightQuoting.Core.Plugins;

public class FreightLogisticsPlugin
{
private readonly QuoteTelemetryContext telemetry;

    public FreightLogisticsPlugin(QuoteTelemetryContext telemetry)
    {
        this.telemetry = telemetry;
    }
  private static readonly Dictionary<string, string> ValidHubs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["chicago"] = "CHI_ORD",
        ["dallas"] = "DAL_DFW",
        ["minneapolis"] = "MSP_HQ",
        ["atlanta"] = "ATL_HUB"
    };

    private static readonly Dictionary<(string, string), int> DistanceMatrix = new()
    {
        [("CHI_ORD", "DAL_DFW")] = 925,
        [("MSP_HQ", "CHI_ORD")] = 408,
        [("DAL_DFW", "ATL_HUB")] = 780
    };

    [KernelFunction("validate_locations")]
    [Description("Validates origin and destination cities against approved logistics hubs. Returns hub codes or flags missing info.")]
    public string ValidateLocations(
        [Description("Origin city name")] string? originCity,
        [Description("Destination city name")] string? destinationCity)
    {
        if (string.IsNullOrWhiteSpace(originCity) || !ValidHubs.TryGetValue(originCity.Trim(), out var originCode) ||
            string.IsNullOrWhiteSpace(destinationCity) || !ValidHubs.TryGetValue(destinationCity.Trim(), out var destCode))
        {
            telemetry.NeedsClarification = true;
            telemetry.ClarificationMessage = "Missing or unapproved hub location. Please specify exact origin and destination cities (e.g., Chicago, Dallas, Minneapolis, Atlanta).";
            telemetry.LogStep("Halted_Missing_Or_Invalid_Hub");
            return JsonSerializer.Serialize(new { status = "HALTED", reason = telemetry.ClarificationMessage });
        }

        telemetry.LogStep($"Hubs_Validated:{originCode}->{destCode}");
        return JsonSerializer.Serialize(new { status = "VALID", originCode, destCode });
    }

    [KernelFunction("calculate_compliance_surcharge")]
    [Description("Determines required equipment type and compliance surcharges based on cargo requirements.")]
    public string CalculateComplianceSurcharge(
        [Description("True if cargo is medical, frozen, perishable, or temperature-controlled")] bool requiresTempControl)
    {
        if (requiresTempControl)
        {
            telemetry.LogStep("Compliance_Reefer_Surcharge_Applied($500)");
            return JsonSerializer.Serialize(new { equipment = "Reefer (Temp-Controlled)", surchargeUsd = 500.00m });
        }

        telemetry.LogStep("Compliance_Standard_DryVan");
        return JsonSerializer.Serialize(new { equipment = "DryVan (Standard)", surchargeUsd = 0.00m });
    }

    [KernelFunction("get_contract_rate")]
    [Description("Queries primary contract freight rate API. May throw HTTP 500 timeout requiring fallback.")]
    public string GetContractRate(string originCode, string destinationCode)
    {
        // Chaos Monkey: 30% simulated API failure
        if (Random.Shared.NextDouble() < 0.30)
        {
            telemetry.LogStep("ERROR_500_Contract_API_Timeout");
            throw new HttpRequestException("500 Internal Server Error: Primary Contract Rate API timed out.");
        }

        int miles = DistanceMatrix.GetValueOrDefault((originCode, destinationCode), 500);
        decimal rate = miles * 2.50m;
        telemetry.LogStep($"Contract_Rate_Locked(${rate})");
        return JsonSerializer.Serialize(new { source = "PrimaryContractAPI", miles, baseRateUsd = rate });
    }

    [KernelFunction("get_spot_market_fallback_rate")]
    [Description("Fallback spot market pricing API. Call this ONLY if get_contract_rate fails or throws an exception.")]
    public string GetSpotMarketFallbackRate(string originCode, string destinationCode)
    {
        int miles = DistanceMatrix.GetValueOrDefault((originCode, destinationCode), 500);
        decimal spotRate = miles * 3.00m; // 20% spot market premium
        telemetry.LogStep($"Fallback_Spot_Rate_Recovered(${spotRate})");
        return JsonSerializer.Serialize(new { source = "SpotMarketFallbackAPI", miles, baseRateUsd = spotRate });
    }
}
