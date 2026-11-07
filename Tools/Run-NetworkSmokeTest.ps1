param(
    [string]$Executable = (Join-Path $PSScriptRoot '../Builds/DesktopNetworkTest/SafeShelter.exe'),
    [ValidateRange(1, 65535)][int]$Port = 17777
)

$ErrorActionPreference = 'Stop'
$testExecutable = (Resolve-Path -LiteralPath $Executable).Path
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$resultsPath = Join-Path $repoRoot ('Temp/NetworkingSmoke-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $resultsPath -Force | Out-Null
$probeHost = $null
$probeClient = $null

function Wait-ForMarker([string]$Marker, [int]$TimeoutSeconds) {
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while (-not (Test-Path -LiteralPath (Join-Path $resultsPath $Marker))) {
        foreach ($failure in @('host-fail', 'client-fail')) {
            $failurePath = Join-Path $resultsPath $failure
            if (Test-Path -LiteralPath $failurePath) {
                throw (Get-Content -LiteralPath $failurePath -Raw)
            }
        }
        if ([DateTime]::UtcNow -gt $deadline) { throw "Timed out waiting for $Marker. Logs: $resultsPath" }
        if ($probeHost.HasExited -and -not (Test-Path -LiteralPath (Join-Path $resultsPath 'host-pass'))) {
            throw "Host exited before passing. Logs: $resultsPath"
        }
        if ($null -ne $probeClient -and $probeClient.HasExited -and
            -not (Test-Path -LiteralPath (Join-Path $resultsPath 'client-pass'))) {
            throw "Client exited before passing. Logs: $resultsPath"
        }
        Start-Sleep -Milliseconds 500
    }
}

try {
    $commonArgs = @('-batchmode', '-nographics', '--safeshelter-address', '127.0.0.1', '--safeshelter-port', "$Port",
        '--safeshelter-smoke', ('"{0}"' -f $resultsPath))
    $probeHost = Start-Process -FilePath $testExecutable -ArgumentList ($commonArgs + @(
        '--safeshelter-role', 'host', '-logFile', ('"{0}"' -f (Join-Path $resultsPath 'host.log'))
    )) -WindowStyle Hidden -PassThru
    Wait-ForMarker 'host-ready' 120
    $probeClient = Start-Process -FilePath $testExecutable -ArgumentList ($commonArgs + @(
        '--safeshelter-role', 'client', '-logFile', ('"{0}"' -f (Join-Path $resultsPath 'client.log'))
    )) -WindowStyle Hidden -PassThru
    Wait-ForMarker 'host-pass' 120
    Wait-ForMarker 'client-pass' 120
    Write-Output 'PASS: late join, spawn, client write rejection, live movement/rotation, removal, reset, keep-clear zones (when configured), disconnect and reload.'
    Write-Output "Logs: $resultsPath"
}
finally {
    foreach ($process in @($probeHost, $probeClient)) {
        if ($null -ne $process -and -not $process.HasExited) { Stop-Process -Id $process.Id }
    }
}
