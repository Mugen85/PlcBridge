using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlcBridge.Core.Interfaces;
using PlcBridge.Infrastructure.Services;
using PlcBridge.WebHmi.Components;
using PlcBridge.WebHmi.Services;
using PlcBridge.Core.Models;

var builder = WebApplication.CreateBuilder(args);

// PRIMA (collegamento diretto abusivo):
// builder.Services.AddSingleton<IPlcService, SimulatedPlcService>();

// ORA (collegamento pulito via rete TCP):
builder.Services.AddSingleton<IPlcService, NetworkPlcService>();

// 2. Aggiunta dei componenti Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

    builder.Services.Configure<TcpSettings>(builder.Configuration.GetSection(TcpSettings.SectionName));
builder.Services.AddSingleton<IPlcService, NetworkPlcService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();