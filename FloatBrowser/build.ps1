$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "src\FloatBrowser.App\FloatBrowser.App.csproj"
$outDir = Join-Path $root "publish"

Write-Host "Publishing FloatBrowser to: $outDir"
dotnet publish $project -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -o $outDir

Write-Host "Publish completed."
Write-Host "Output: $(Join-Path $outDir 'FloatBrowser.App.exe')"

