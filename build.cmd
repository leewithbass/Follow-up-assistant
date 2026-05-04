@echo off
chcp 65001 >nul
echo Building FloatBrowser...

dotnet publish src\FloatBrowser.App\FloatBrowser.App.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -o publish

if %errorlevel% equ 0 (
    echo.
    echo Build succeeded!
    echo Output: .\publish\FloatBrowser.App.exe
) else (
    echo.
    echo Build failed.
    exit /b %errorlevel%
)