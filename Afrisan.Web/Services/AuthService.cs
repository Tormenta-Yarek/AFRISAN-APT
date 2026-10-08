using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Afrisan.Web.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private readonly JwtAuthenticationStateProvider _authStateProvider;

        private const string TokenKey = "afrisan_token";

        public AuthService(
            HttpClient httpClient,
            IJSRuntime jsRuntime,
            JwtAuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
            _authStateProvider = authStateProvider;
        }

        public async Task<LoginResultado> LoginAsync(
            string email,
            string password)
        {
            try
            {
                var respuesta = await _httpClient.PostAsJsonAsync(
                    "api/Auth/login",
                    new
                    {
                        email,
                        password
                    });

                if (!respuesta.IsSuccessStatusCode)
                {
                    var contenido =
                        await respuesta.Content.ReadAsStringAsync();

                    try
                    {
                        var error =
                            JsonSerializer.Deserialize<ErrorRespuesta>(
                                contenido,
                                new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });

                        return new LoginResultado
                        {
                            Exitoso = false,
                            Mensaje =
                                error?.Mensaje ??
                                "No fue posible iniciar sesión."
                        };
                    }
                    catch
                    {
                        return new LoginResultado
                        {
                            Exitoso = false,
                            Mensaje =
                                "Correo o contraseña incorrectos."
                        };
                    }
                }

                var resultado =
                    await respuesta.Content.ReadFromJsonAsync<LoginRespuesta>(
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (resultado == null ||
                    string.IsNullOrWhiteSpace(resultado.Token))
                {
                    return new LoginResultado
                    {
                        Exitoso = false,
                        Mensaje =
                            "La API no devolvió un token válido."
                    };
                }

                await _jsRuntime.InvokeVoidAsync(
                    "localStorage.setItem",
                    TokenKey,
                    resultado.Token);

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        resultado.Token);

                await _authStateProvider.MarcarUsuarioAutenticadoAsync(
                    resultado.Token);

                return new LoginResultado
                {
                    Exitoso = true,
                    Mensaje = "Inicio de sesión correcto."
                };
            }
            catch
            {
                return new LoginResultado
                {
                    Exitoso = false,
                    Mensaje =
                        "No se pudo conectar con la API de AFRISAN."
                };
            }
        }

        public async Task LogoutAsync()
        {
            await _jsRuntime.InvokeVoidAsync(
                "localStorage.removeItem",
                TokenKey);

            _httpClient.DefaultRequestHeaders.Authorization = null;

            _authStateProvider.MarcarUsuarioNoAutenticado();
        }
    }

    public class LoginResultado
    {
        public bool Exitoso { get; set; }

        public string Mensaje { get; set; } = string.Empty;
    }

    public class LoginRespuesta
    {
        public string Mensaje { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;

        public DateTime Expiracion { get; set; }

        public UsuarioLogin? Usuario { get; set; }
    }

    public class UsuarioLogin
    {
        public string Id { get; set; } = string.Empty;

        public string NombreCompleto { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = new();
    }

    public class ErrorRespuesta
    {
        public string Mensaje { get; set; } = string.Empty;
    }
}