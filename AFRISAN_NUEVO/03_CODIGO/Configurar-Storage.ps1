[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$api=Join-Path $PSScriptRoot 'Afrisan.Nuevo.Api\Afrisan.Nuevo.Api.csproj'
& dotnet build $api --nologo -m:1 -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'No se pudo compilar la API.'}
Write-Host 'Mantén la API detenida mientras se copian las fotos y se activa Storage.'
& dotnet run --project $api --no-build --launch-profile http -- --configurar-storage
if($LASTEXITCODE -ne 0){throw 'Storage no quedó activado. Revisa el diagnóstico anterior. Los originales locales se conservan.'}
& dotnet run --project $api --no-build --launch-profile http -- --verificar-storage
if($LASTEXITCODE -ne 0){throw 'La comprobación de Storage no fue satisfactoria.'}
