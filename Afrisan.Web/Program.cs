using Afrisan.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configuración de Blazor WebAssembly
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Dirección desde la que se abrió AFRISAN
var direccionWeb = new Uri(builder.HostEnvironment.BaseAddress);

// Dirección de la API
string direccionApi;

if (direccionWeb.Scheme == "https" &&
    direccionWeb.Host.Equals(
        "localhost",
        StringComparison.OrdinalIgnoreCase))
{
    // Conserva el acceso HTTPS que tenías configurado en el PC
    direccionApi = "https://localhost:7290/";
}
else
{
    // Usa automáticamente el mismo nombre o IP con que se abrió la web
    direccionApi = $"http://{direccionWeb.Host}:5141/";
}

// Registrar HttpClient para consultar la API
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(direccionApi)
});

await builder.Build().RunAsync();