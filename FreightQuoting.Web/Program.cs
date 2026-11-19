using FreightQuoting.Core;
using FreightQuoting.Core.Models;
using FreightQuoting.Core.Services;
using Microsoft.Extensions.Options;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => options.IdleTimeout = TimeSpan.FromMinutes(30));

// Bind & validate User Secrets / appsettings via IOptions
builder.Services.AddOptions<AzureOpenAIOptions>()
    .Bind(builder.Configuration.GetSection(AzureOpenAIOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Endpoint) && !string.IsNullOrWhiteSpace(o.ApiKey),
        "AzureOpenAI Endpoint and ApiKey must be configured in User Secrets.")
    .ValidateOnStart();

builder.Services.AddSingleton<IQuoteOrchestratorService>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;
    return new QuoteOrchestratorService(opts.Endpoint, opts.ApiKey, opts.Deployment);
});

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapDefaultControllerRoute();
app.Run();