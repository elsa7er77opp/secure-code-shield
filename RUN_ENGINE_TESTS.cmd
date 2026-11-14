@echo off
REM Builds the engine plus the new certificate work and runs the whole suite.
setlocal
cd /d "%~dp0"

echo === build solution ===
dotnet build SecureCodeShield.slnx -c Release --nologo
if errorlevel 1 goto :fail

echo.
echo === all tests ===
dotnet test SecureCodeShield.slnx -c Release --nologo
if errorlevel 1 goto :fail

echo.
echo === publish the standalone verifier ===
dotnet publish src\SecureCodeShield.Verifier\SecureCodeShield.Verifier.csproj -c Release -o artifacts\verifier --nologo
if errorlevel 1 goto :fail

echo.
echo ALL GREEN
echo Verifier: artifacts\verifier\scs-verify.exe
goto :eof

:fail
echo.
echo FAILED - copy the output above back into the chat.
exit /b 1
