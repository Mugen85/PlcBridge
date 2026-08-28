using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlcBridge.Core.Interfaces;
using PlcBridge.Infrastructure.Services;
using PlcBridge.WebHmi.Components;

var builder = WebApplication.CreateBuilder(args);

// 1. Registrazione dei servizi di dominio (Clean Architecture)
builder.Services.AddSingleton<IPlcService, SimulatedPlcService>();

// 2. Aggiunta dei componenti Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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