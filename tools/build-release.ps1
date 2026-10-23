param([string]$OutputDirectory = '')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $projectRoot 'dist' }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$stageRoot = Join-Path $OutputDirectory ('stage-' + [Guid]::NewGuid().ToString('N'))
$packageRoot = Join-Path $stageRoot 'HelenaPet'
New-Item -ItemType Directory -Path $packageRoot | Out-Null
$required = @('HelenaPet.exe','Start.cmd','启动盲女.cmd','开始使用.txt','settings.json','interaction.json','dialogue.json','assets','skins')
foreach ($name in $required) {
    $sourcePath = Join-Path $projectRoot $name
    if (-not (Test-Path -LiteralPath $sourcePath)) { throw "Required runtime file missing: $name" }
    Copy-Item -LiteralPath $sourcePath -Destination $packageRoot -Recurse
}
# Ship clean defaults instead of the maintainer's coordinates and outfit.
[IO.File]::WriteAllText((Join-Path $packageRoot 'settings.json'), "{}`r`n", (New-Object Text.UTF8Encoding($false)))
$skinCount = (Get-ChildItem -LiteralPath (Join-Path $packageRoot 'skins') -Directory | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'rig.json') }).Count
if ($skinCount -ne 16) { throw "Expected 16 skin options, got $skinCount" }
$zipPath = Join-Path $OutputDirectory 'HelenaPet-Windows.zip'
if (Test-Path -LiteralPath $zipPath) { throw "Output already exists. Use a new OutputDirectory: $zipPath" }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($stageRoot, $zipPath, [IO.Compression.CompressionLevel]::Optimal, $false)
$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
$exeHash = (Get-FileHash -LiteralPath (Join-Path $packageRoot 'HelenaPet.exe') -Algorithm SHA256).Hash.ToLowerInvariant()
$checksumText = "$zipHash  HelenaPet-Windows.zip`n$exeHash  HelenaPet/HelenaPet.exe`n"
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'SHA256SUMS.txt'), $checksumText, (New-Object Text.UTF8Encoding($false)))
[pscustomobject]@{Archive=$zipPath;Bytes=(Get-Item -LiteralPath $zipPath).Length;SkinOptions=$skinCount;SHA256=$zipHash;ExecutableSHA256=$exeHash}
# Keep staging for inspection. dist/ is ignored by Git.
