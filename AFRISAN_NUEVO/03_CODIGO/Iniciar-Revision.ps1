param([switch]$NoAbrir, [switch]$Detener)

$ErrorActionPreference = 'Stop'
$raizRevision = $PSScriptRoot
$logsRevision = Join-Path $raizRevision '.local'
New-Item -ItemType Directory -Path $logsRevision -Force | Out-Null
$servidoresRevision = @(
    @{ Nombre='api'; Proyecto='Afrisan.Nuevo.Api'; Puerto=5181; Ruta='health/ready' },
    @{ Nombre='web'; Proyecto='Afrisan.Nuevo.Web'; Puerto=5286; Ruta='acceso' }
)

function Detener-ArbolRevision($proceso) {
    if ($proceso.HasExited) { return }
    taskkill.exe /PID $proceso.Id /T /F | Out-Null
    if ($LASTEXITCODE -ne 0 -and -not $proceso.HasExited) {
        throw "No se pudo detener el proceso $($proceso.Id)."
    }
}

function Detener-ServidorRevision($servidor) {
    $archivo = Join-Path $logsRevision "$($servidor.Nombre).pid.json"
    if (-not (Test-Path -LiteralPath $archivo)) { return }
    $registro = Get-Content -LiteralPath $archivo -Raw | ConvertFrom-Json
    $proceso = Get-Process -Id $registro.Id -ErrorAction SilentlyContinue
    if ($proceso -and $proceso.StartTime.ToUniversalTime().Ticks.ToString() -eq $registro.Inicio) {
        Detener-ArbolRevision $proceso
    }
    Remove-Item -LiteralPath $archivo
}

function Confirmar-ServidorRevision($servidor) {
    try {
        $contenido = $clienteRevision.GetStringAsync("http://localhost:$($servidor.Puerto)/$($servidor.Ruta)").GetAwaiter().GetResult()
        if ($servidor.Nombre -eq 'api') {
            $estado = $contenido | ConvertFrom-Json
            return ($estado.almacenamiento -eq 'Memoria' -or
                ($estado.almacenamiento -eq 'PostgreSQL' -and $estado.supabase -eq $true))
        }
        return ($contenido -match '<title>[^<]*AFRISAN' -and $contenido -match 'blazor\.webassembly\.js')
    } catch {
        return $false
    }
}

function Esperar-ServidorRevision($servidor, $proceso) {
    $limite = [DateTime]::UtcNow.AddSeconds(40)
    do {
        if (Confirmar-ServidorRevision $servidor) { return }
        if ($proceso -and $proceso.HasExited) { break }
        Start-Sleep -Milliseconds 600
    } while ([DateTime]::UtcNow -lt $limite)
    $log = Join-Path $logsRevision "$($servidor.Nombre)-error.log"
    throw "AFRISAN no respondió en el puerto $($servidor.Puerto). Revisa $log o la salida del proyecto en Visual Studio."
}

try {
    $bloqueoRevision = [System.IO.File]::Open(
        (Join-Path $logsRevision 'inicio.lock'), 'OpenOrCreate', 'ReadWrite', 'None')
} catch [System.IO.IOException] {
    throw 'Ya hay otro arranque o detención de AFRISAN en curso.'
}

$clienteRevision = $null
$iniciadosRevision = @()
try {
    if ($Detener) {
        foreach ($servidor in $servidoresRevision) { Detener-ServidorRevision $servidor }
        Write-Host 'Se detuvieron los servidores iniciados con este script.'
        return
    }

    Add-Type -AssemblyName System.Net.Http
    $handlerRevision = [System.Net.Http.HttpClientHandler]::new()
    $handlerRevision.UseProxy = $false
    $clienteRevision = [System.Net.Http.HttpClient]::new($handlerRevision)
    $clienteRevision.Timeout = [TimeSpan]::FromSeconds(4)

    $faltantesRevision = @(
        foreach ($servidor in $servidoresRevision) {
            if (Confirmar-ServidorRevision $servidor) { continue }
            if (Get-NetTCPConnection -State Listen -LocalPort $servidor.Puerto -ErrorAction SilentlyContinue) {
                Esperar-ServidorRevision $servidor $null
            } else {
                $servidor
            }
        }
    )

    if ($faltantesRevision.Count -gt 0) {
        $compilarRevision = if ($faltantesRevision.Count -eq 2) { 'Afrisan.Nuevo.slnx' } else { $faltantesRevision[0].Proyecto }
        Push-Location $raizRevision
        try {
            dotnet build $compilarRevision --nologo -m:1 -p:UseSharedCompilation=false
            if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar AFRISAN.' }
        } finally { Pop-Location }
    }

    foreach ($servidor in $faltantesRevision) {
        if (Confirmar-ServidorRevision $servidor) { continue }
        $proceso = Start-Process -FilePath dotnet -WorkingDirectory $raizRevision -WindowStyle Hidden -PassThru `
            -ArgumentList "run --project $($servidor.Proyecto) --no-build --no-restore --launch-profile http" `
            -RedirectStandardOutput (Join-Path $logsRevision "$($servidor.Nombre).log") `
            -RedirectStandardError (Join-Path $logsRevision "$($servidor.Nombre)-error.log")
        $iniciadosRevision += @{ Proceso=$proceso; Servidor=$servidor }
        @{ Id=$proceso.Id; Inicio=$proceso.StartTime.ToUniversalTime().Ticks.ToString() } |
            ConvertTo-Json -Compress | Set-Content -LiteralPath (Join-Path $logsRevision "$($servidor.Nombre).pid.json")
        Esperar-ServidorRevision $servidor $proceso
    }
} catch {
    foreach ($inicio in $iniciadosRevision) {
        try {
            Detener-ArbolRevision $inicio.Proceso
            $archivo = Join-Path $logsRevision "$($inicio.Servidor.Nombre).pid.json"
            if (Test-Path -LiteralPath $archivo) { Remove-Item -LiteralPath $archivo }
        } catch {
            Write-Warning $_.Exception.Message
        }
    }
    throw
} finally {
    if ($clienteRevision) { $clienteRevision.Dispose() }
    $bloqueoRevision.Dispose()
}

Write-Host 'AFRISAN disponible: http://localhost:5286/acceso'
Write-Host 'Estado de la API: http://localhost:5181/health/ready'
if (-not $NoAbrir) { Start-Process 'http://localhost:5286/acceso' }
