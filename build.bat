@echo off
echo Building SwitchBot Home Control (C# Edition)...
echo.

REM Clean previous builds
if exist bin\Release rmdir /s /q bin\Release

REM Build single-file executable
dotnet publish SwitchBotHomeControl.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

echo.
echo Build completed!
echo Executable: bin\Release\net8.0-windows\win-x64\publish\SwitchBotHomeControl.exe
echo.
echo Don't forget to copy .env file to the publish folder!
pause
