using Afrisan.Nuevo.Contratos;

namespace Afrisan.Nuevo.Api.PowerBi;

public sealed record PowerBiOptions(Guid? ReportId, Guid? TenantId)
{
    public static PowerBiOptions Load(IConfiguration configuration)
    {
        static Guid? Read(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (!Guid.TryParse(value, out var id) || id == Guid.Empty)
                throw new InvalidOperationException("Power BI requiere identificadores GUID válidos. Revisa ReportId y TenantId.");
            return id;
        }
        return new(Read(configuration["PowerBi:ReportId"]), Read(configuration["PowerBi:TenantId"]));
    }

    public PowerBiConfiguracionResponse Publica()
    {
        if (ReportId is null) return new(false, null, null);
        // No se aceptan URLs arbitrarias ni Publish to web (/view?r=...).
        var url = $"https://app.powerbi.com/reportEmbed?reportId={ReportId:D}&autoAuth=true";
        if (TenantId is not null) url += $"&ctid={TenantId:D}";
        return new(true, url, $"https://app.powerbi.com/groups/me/reports/{ReportId:D}");
    }
}
