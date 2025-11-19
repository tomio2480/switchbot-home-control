@echo off
echo Starting SwitchBot Home Control (C# Edition)...
echo.

REM Check if .env exists
if not exist .env (
    echo ERROR: .env file not found!
    echo Please copy .env.example to .env and configure your API credentials.
    pause
    exit /b 1
)

REM Run the application
dotnet run --project SwitchBotHomeControl.csproj

pause
