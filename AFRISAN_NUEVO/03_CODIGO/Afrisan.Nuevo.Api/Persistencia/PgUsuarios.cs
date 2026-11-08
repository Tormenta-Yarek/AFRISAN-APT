using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Afrisan.Nuevo.Contratos;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Afrisan.Nuevo.Api.Persistencia;

public sealed class PgUsuarios(PgDatabase database, PersistenciaOptions settings)
{
    public const string Issuer = "AFRISAN.Nuevo.Api";
    public const string Audience = "AFRISAN.Nuevo.Web";
    private readonly PasswordHasher<string> hasher = new();
    private const string Select = """
        SELECT u."Id_Usuario" AS "Id",u."Nombre",u."Apellido",u."Correo_Usuario" AS "Correo",
          u."Clave_Usuario" AS "Hash",r."Nombre_Rol" AS "Rol",u."Activo",u."IntentosFallidos" AS "Intentos",
          u."BloqueadoHasta",u."VersionSeguridad" AS "Version"
        FROM afrisan."Usuario" u JOIN afrisan."Rol" r USING ("Id_Rol")
        """;
    public async Task<UsuarioRow?> User(int id)
    {
        await using var unit = await database.Open();
        return (await unit.Query<UsuarioRow>(Select + " WHERE u.\"Id_Usuario\"=@p0", id)).SingleOrDefault();
    }
    public async Task<SesionResponse?> Login(LoginRequest request)
    {
        await using var unit = await database.Open();
        var user = (await unit.Query<UsuarioRow>(Select + " WHERE lower(btrim(u.\"Correo_Usuario\"))=@p0 FOR UPDATE OF u", request.Email.Trim().ToLowerInvariant())).SingleOrDefault();
        if (user is null || !user.Activo || user.BloqueadoHasta > DateTimeOffset.UtcNow) return null;
        PasswordVerificationResult valid;
        try { valid = hasher.VerifyHashedPassword(user.Correo, user.Hash, request.Password); }
        catch (FormatException) { valid = PasswordVerificationResult.Failed; }
        if (valid == PasswordVerificationResult.Failed)
        {
            await unit.Exec("""
                UPDATE afrisan."Usuario" SET "IntentosFallidos"="IntentosFallidos"+1,
                  "BloqueadoHasta"=CASE WHEN "IntentosFallidos"+1>=5 THEN clock_timestamp()+interval '15 minutes' ELSE NULL END
                WHERE "Id_Usuario"=@p0
                """, user.Id);
            await unit.Commit(); return null;
        }
        await unit.Exec("""
            UPDATE afrisan."Usuario" SET "IntentosFallidos"=0,"BloqueadoHasta"=NULL,"Clave_Usuario"=@p1 WHERE "Id_Usuario"=@p0
            """, user.Id, valid == PasswordVerificationResult.SuccessRehashNeeded ? hasher.HashPassword(user.Correo, request.Password) : user.Hash);
        await unit.Commit();
        var expires = DateTimeOffset.UtcNow.AddHours(2);
        var jwt = new JwtSecurityToken(Issuer, Audience,
            [new(ClaimTypes.NameIdentifier,user.Id.ToString()), new(ClaimTypes.Name,$"{user.Nombre} {user.Apellido}"),
             new(ClaimTypes.Role,user.Rol), new("correo",user.Correo), new("version",user.Version.ToString()), new("jti",Guid.NewGuid().ToString())],
            expires: expires.UtcDateTime, signingCredentials: new(new SymmetricSecurityKey(settings.JwtKey),SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(jwt), expires, $"{user.Nombre} {user.Apellido}",user.Rol,user.Correo);
    }
    public async Task<UsuarioResponse[]> Listar()
    {
        await using var unit = await database.Open();
        return (await unit.Query<UsuarioRow>(Select + " ORDER BY u.\"Nombre\",u.\"Apellido\""))
            .Select(u => new UsuarioResponse(u.Id,u.Nombre,u.Apellido,u.Correo,u.Rol,u.Activo)).ToArray();
    }
    public async Task<UsuarioResponse> Crear(CrearUsuarioRequest request, int? actor, bool inicial = false)
    {
        if (!Roles.Todos.Contains(request.Rol) || request.Password.Length is < 12 or > 128)
            throw new ReglaInventarioException("Rol o longitud de contraseña inválidos.");
        await using var unit = await database.Open(actor);
        if (inicial)
        {
            await unit.Query<long>("SELECT 1::bigint AS \"Value\" FROM pg_advisory_xact_lock(73420261102)");
            if (await unit.One<long>("SELECT count(*) AS \"Value\" FROM afrisan.\"Usuario\"") != 0)
                throw new ReglaInventarioException("Ya hay usuarios. El primer administrador no puede crearse otra vez.",409);
            foreach (var role in Roles.Todos)
                await unit.Exec("INSERT INTO afrisan.\"Rol\"(\"Nombre_Rol\") VALUES(@p0) ON CONFLICT DO NOTHING",role);
            if (request.Rol != "Administrador") throw new ReglaInventarioException("El usuario inicial debe ser Administrador.");
        }
        var correo = request.Correo.Trim().ToLowerInvariant();
        var id = await unit.One<int>("""
            INSERT INTO afrisan."Usuario"("Nombre","Apellido","Correo_Usuario","Clave_Usuario","Id_Rol")
            SELECT @p0,@p1,@p2,@p3,"Id_Rol" FROM afrisan."Rol" WHERE "Nombre_Rol"=@p4 RETURNING "Id_Usuario" AS "Value"
            """,request.Nombre.Trim(),request.Apellido.Trim(),correo,hasher.HashPassword(correo,request.Password),request.Rol);
        await unit.Commit();
        return new(id,request.Nombre.Trim(),request.Apellido.Trim(),correo,request.Rol,true);
    }
}
