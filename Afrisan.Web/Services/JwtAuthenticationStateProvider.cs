using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Afrisan.Web.Services
{
    public class JwtAuthenticationStateProvider
        : AuthenticationStateProvider
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly HttpClient _httpClient;

        private const string TokenKey = "afrisan_token";

        private static readonly ClaimsPrincipal UsuarioAnonimo =
            new ClaimsPrincipal(
                new ClaimsIdentity());

        public JwtAuthenticationStateProvider(
            IJSRuntime jsRuntime,
            HttpClient httpClient)
        {
            _jsRuntime = jsRuntime;
            _httpClient = httpClient;
        }

        public override async Task<AuthenticationState>
            GetAuthenticationStateAsync()
        {
            try
            {
                var token =
                    await _jsRuntime.InvokeAsync<string?>(
                        "localStorage.getItem",
                        TokenKey);

                if (string.IsNullOrWhiteSpace(token))
                {
                    return new AuthenticationState(
                        UsuarioAnonimo);
                }

                var claims = ObtenerClaims(token);

                if (TokenExpirado(claims))
                {
                    await _jsRuntime.InvokeVoidAsync(
                        "localStorage.removeItem",
                        TokenKey);

                    _httpClient
                        .DefaultRequestHeaders
                        .Authorization = null;

                    return new AuthenticationState(
                        UsuarioAnonimo);
                }

                _httpClient
                    .DefaultRequestHeaders
                    .Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        token);

                var identity =
                    new ClaimsIdentity(
                        claims,
                        authenticationType: "jwt",
                        nameType: ClaimTypes.Name,
                        roleType: ClaimTypes.Role);

                var usuario =
                    new ClaimsPrincipal(identity);

                return new AuthenticationState(
                    usuario);
            }
            catch
            {
                return new AuthenticationState(
                    UsuarioAnonimo);
            }
        }

        public Task MarcarUsuarioAutenticadoAsync(
            string token)
        {
            var claims = ObtenerClaims(token);

            var identity =
                new ClaimsIdentity(
                    claims,
                    authenticationType: "jwt",
                    nameType: ClaimTypes.Name,
                    roleType: ClaimTypes.Role);

            var usuario =
                new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(
                Task.FromResult(
                    new AuthenticationState(
                        usuario)));

            return Task.CompletedTask;
        }

        public void MarcarUsuarioNoAutenticado()
        {
            _httpClient
                .DefaultRequestHeaders
                .Authorization = null;

            NotifyAuthenticationStateChanged(
                Task.FromResult(
                    new AuthenticationState(
                        UsuarioAnonimo)));
        }

        private static List<Claim> ObtenerClaims(
            string token)
        {
            var partes = token.Split('.');

            if (partes.Length != 3)
            {
                throw new InvalidOperationException(
                    "El token JWT no tiene un formato válido.");
            }

            var payload = partes[1];

            var jsonBytes =
                ParseBase64SinPadding(payload);

            var pares =
                JsonSerializer.Deserialize<
                    Dictionary<string, JsonElement>>(
                    jsonBytes);

            var claims =
                new List<Claim>();

            if (pares == null)
            {
                return claims;
            }

            foreach (var par in pares)
            {
                if (par.Value.ValueKind ==
                    JsonValueKind.Array)
                {
                    foreach (var elemento
                        in par.Value.EnumerateArray())
                    {
                        claims.Add(
                            new Claim(
                                par.Key,
                                elemento.ToString()));
                    }
                }
                else
                {
                    claims.Add(
                        new Claim(
                            par.Key,
                            par.Value.ToString()));
                }
            }

            return claims;
        }

        private static bool TokenExpirado(
            IEnumerable<Claim> claims)
        {
            var exp =
                claims.FirstOrDefault(
                    c => c.Type == "exp")
                ?.Value;

            if (string.IsNullOrWhiteSpace(exp))
            {
                return true;
            }

            if (!long.TryParse(
                exp,
                out var segundosUnix))
            {
                return true;
            }

            var fechaExpiracion =
                DateTimeOffset
                    .FromUnixTimeSeconds(
                        segundosUnix);

            return fechaExpiracion <=
                   DateTimeOffset.UtcNow;
        }

        private static byte[]
            ParseBase64SinPadding(
                string base64)
        {
            base64 =
                base64
                    .Replace('-', '+')
                    .Replace('_', '/');

            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;

                case 3:
                    base64 += "=";
                    break;
            }

            return Convert.FromBase64String(
                base64);
        }
    }
}