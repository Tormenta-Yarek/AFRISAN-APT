[CmdletBinding()]
param([switch]$SoloAdministrador)
$ErrorActionPreference='Stop'
$api=Join-Path $PSScriptRoot 'Afrisan.Nuevo.Api\Afrisan.Nuevo.Api.csproj'
& dotnet build $api --nologo -m:1 -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'No se pudo compilar la API.'}
if(-not $SoloAdministrador){
    & dotnet run --project $api --no-build --launch-profile http -- --preparar-persistencia
    if($LASTEXITCODE -ne 0){throw 'No se pudo preparar la conexión persistente. Revisa el diagnóstico anterior.'}
}
& dotnet run --project $api --no-build --launch-profile http -- --crear-administrador
if($LASTEXITCODE -ne 0){throw 'No se creó el administrador. Revisa el resultado anterior.'}
