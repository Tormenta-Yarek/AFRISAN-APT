using Afrisan.Web;
using Afrisan.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// =====================================================
// BLAZOR
// =====================================================

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// =====================================================
// DIRECCIÓN DE AFRISAN.API
// =====================================================

var direccionWeb =
    new Uri(builder.HostEnvironment.BaseAddress);

string direccionApi;

if (direccionWeb.Scheme == "https" &&
    direccionWeb.Host.Equals(
        "localhost",
        StringComparison.OrdinalIgnoreCase))
{
    direccionApi = "https://localhost:7290/";
}
else
{
    direccionApi =
        $"http://{direccionWeb.Host}:5141/";
}

// =====================================================
// HTTP CLIENT
// =====================================================

builder.Services.AddScoped(
    sp => new HttpClient
    {
        BaseAddress =
            new Uri(direccionApi)
    });

// =====================================================
// AUTENTICACIÓN Y AUTORIZACIÓN
// =====================================================

builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<
    JwtAuthenticationStateProvider>();

builder.Services.AddScoped<
    AuthenticationStateProvider>(
        sp => sp.GetRequiredService<
            JwtAuthenticationStateProvider>());

builder.Services.AddScoped<AuthService>();

// =====================================================
// EJECUTAR APLICACIÓN
// =====================================================

await builder.Build().RunAsync();