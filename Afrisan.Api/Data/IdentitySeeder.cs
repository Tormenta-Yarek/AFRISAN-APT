using Afrisan.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace Afrisan.Api.Data
{
    public static class IdentitySeeder
    {
        public static async Task InicializarAsync(
            IServiceProvider services,
            IConfiguration configuration)
        {
            var roleManager =
                services.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager =
                services.GetRequiredService<UserManager<Usuario>>();

            string[] roles =
            {
                "Administrador",
                "Bodega",
                "Tecnico"
            };

            foreach (var rol in roles)
            {
                if (!await roleManager.RoleExistsAsync(rol))
                {
                    var resultadoRol =
                        await roleManager.CreateAsync(
                            new IdentityRole(rol));

                    if (!resultadoRol.Succeeded)
                    {
                        throw new InvalidOperationException(
                            $"No se pudo crear el rol {rol}."
                        );
                    }
                }
            }

            var emailAdmin =
                configuration["AdminSeed:Email"];

            var passwordAdmin =
                configuration["AdminSeed:Password"];

            var nombreAdmin =
                configuration["AdminSeed:NombreCompleto"];

            if (string.IsNullOrWhiteSpace(emailAdmin) ||
                string.IsNullOrWhiteSpace(passwordAdmin) ||
                string.IsNullOrWhiteSpace(nombreAdmin))
            {
                return;
            }

            var admin =
                await userManager.FindByEmailAsync(emailAdmin);

            if (admin == null)
            {
                admin = new Usuario
                {
                    UserName = emailAdmin,
                    Email = emailAdmin,
                    NombreCompleto = nombreAdmin,
                    Activo = true,
                    EmailConfirmed = true
                };

                var resultadoUsuario =
                    await userManager.CreateAsync(
                        admin,
                        passwordAdmin);

                if (!resultadoUsuario.Succeeded)
                {
                    var errores = string.Join(
                        " | ",
                        resultadoUsuario.Errors.Select(
                            e => e.Description));

                    throw new InvalidOperationException(
                        $"No se pudo crear el administrador: {errores}"
                    );
                }
            }

            if (!await userManager.IsInRoleAsync(
                admin,
                "Administrador"))
            {
                await userManager.AddToRoleAsync(
                    admin,
                    "Administrador");
            }
        }
    }
}