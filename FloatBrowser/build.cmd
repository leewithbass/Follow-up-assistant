@echo off
setlocal

set "ROOT=%~dp0"
set "PROJECT=%ROOT%src\FloatBrowser.App\FloatBrowser.App.csproj"
set "OUTDIR=%ROOT%publish"

echo Publishing FloatBrowser to: "%OUTDIR%"
dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -o "%OUTDIR%"
if errorlevel 1 (
  echo Publish failed.
  exit /b 1
)

echo Publish completed.
echo Output: "%OUTDIR%\FloatBrowser.App.exe"
endlocal

