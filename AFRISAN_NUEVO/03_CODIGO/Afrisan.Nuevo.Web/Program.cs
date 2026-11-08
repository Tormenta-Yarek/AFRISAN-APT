using Afrisan.Nuevo.Web;
using Afrisan.Nuevo.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5181/api/v1/";
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<SesionService>();
builder.Services.AddScoped<AuthenticationStateProvider>(s => s.GetRequiredService<SesionService>());
builder.Services.AddScoped<InventarioClient>();
builder.Services.AddScoped<TrabajoLocalService>();
var host = builder.Build();
await host.Services.GetRequiredService<SesionService>().InicializarAsync();
await host.RunAsync();
