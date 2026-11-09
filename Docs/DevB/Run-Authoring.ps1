param(
    [ValidateSet('Build', 'BuildAndPackage', 'Validate', 'WireMerged', 'RenderReview')][string]$Action = 'Validate',
    [string]$Unity = 'C:/Program Files/Unity/Hub/Editor/6000.3.27f1/Editor/Unity.exe'
)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$temporary = [IO.Path]::GetFullPath((Join-Path $project 'Assets/DevBAuthoring'))
if (-not $temporary.StartsWith($project + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Authoring folder is outside this project.'
}
if (Get-CimInstance Win32_Process -Filter "name = 'Unity.exe'" | Where-Object { $_.CommandLine -like "*$project*" }) {
    throw 'This project is already open in Unity. Finish that Editor/build session first.'
}
if (Test-Path -LiteralPath $temporary) { throw "Temporary authoring folder already exists: $temporary" }
$logs = Join-Path $project 'Logs/FinalDemoB'
New-Item -ItemType Directory -Force -Path $logs | Out-Null
try {
    New-Item -ItemType Directory -Force -Path (Join-Path $temporary 'Editor') | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'FinalWorldAuthoring.cs') -Destination (Join-Path $temporary 'Editor/FinalWorldAuthoring.cs')
    $arguments = @('-batchmode', '-quit', '-projectPath', ('"{0}"' -f $project),
        '-executeMethod', "FinalWorldAuthoring.$Action", '-logFile', ('"{0}"' -f (Join-Path $logs "$Action.log")))
    if ($Action -eq 'Validate' -or $Action -eq 'WireMerged') { $arguments += '-nographics' }
    $process = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Unity $Action failed ($($process.ExitCode)). See $logs/$Action.log" }
    Write-Output "PASS: $Action. Logs: $logs"
}
finally {
    # Only this invocation's verified temporary directory and its generated meta are removed.
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Recurse -Force }
    if (Test-Path -LiteralPath ($temporary + '.meta')) { Remove-Item -LiteralPath ($temporary + '.meta') -Force }
}
