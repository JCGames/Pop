$ErrorActionPreference = "Stop"

$extensionRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $extensionRoot
$projectPath = Join-Path $repoRoot "Pop.LanguageServer\Pop.LanguageServer.csproj"
$buildOutput = Join-Path $repoRoot "Pop.LanguageServer\bin\Debug\net10.0"
$serverOutput = Join-Path $extensionRoot "server"

dotnet build $projectPath -v q

New-Item -ItemType Directory -Force -Path $serverOutput | Out-Null
Get-ChildItem $serverOutput -File -ErrorAction SilentlyContinue | Remove-Item -Force

$files = @(
    "Pop.LanguageServer.dll",
    "Pop.LanguageServer.deps.json",
    "Pop.LanguageServer.runtimeconfig.json",
    "Pop.Language.dll"
)

foreach ($file in $files) {
    Copy-Item (Join-Path $buildOutput $file) (Join-Path $serverOutput $file) -Force
}

Write-Host "Bundled Pop language server into $serverOutput"
