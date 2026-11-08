param(
    [Guid]$ReportId = [Guid]::Empty,
    [Guid]$TenantId = [Guid]::Empty
)
$ErrorActionPreference = 'Stop'
$proyectoBi = Join-Path $PSScriptRoot 'Afrisan.Nuevo.Api\Afrisan.Nuevo.Api.csproj'
if ($ReportId -eq [Guid]::Empty) {
    $textoBi = Read-Host 'ReportId del informe privado publicado (GUID, no URL)'
    if (-not [Guid]::TryParse($textoBi, [ref]$ReportId) -or $ReportId -eq [Guid]::Empty) {
        throw 'ReportId invalido. Copia el identificador del informe de la insercion segura de Power BI.'
    }
}
# Identificadores, nunca claves ni tokens. El servidor construye una URL Microsoft fija.
& dotnet user-secrets set 'PowerBi:ReportId' $ReportId.ToString('D') --project $proyectoBi
if ($LASTEXITCODE -ne 0) { throw 'No se pudo guardar ReportId.' }
$tenantBi = if ($TenantId -eq [Guid]::Empty) { '' } else { $TenantId.ToString('D') }
if ($tenantBi) {
    & dotnet user-secrets set 'PowerBi:TenantId' $tenantBi --project $proyectoBi
} else {
    & dotnet user-secrets remove 'PowerBi:TenantId' --project $proyectoBi
}
if ($LASTEXITCODE -ne 0) { throw 'No se pudo actualizar TenantId.' }
Write-Host 'Power BI configurado. Reinicia la API y abre /power-bi con Jefatura o Administrador.'
Write-Host 'Microsoft verificara la cuenta y permisos. La configuracion no publica ni comparte el informe.'
