@echo off
setlocal
rem Im Repo-Root ablegen. Startet das Backend, wartet auf Swagger,
rem generiert api-schema.d.ts und beendet das Backend wieder.

set "ROOT=%~dp0"
set "SWAGGER_URL=http://localhost:15421/swagger/v1/swagger.json"
set "ASPNETCORE_ENVIRONMENT=Development"
set /a TRIES=0

echo [1/3] Starte Backend...
start "LV-Backend" /D "%ROOT%" cmd /c dotnet run --project LegendsViewer.Backend\LegendsViewer.Backend.csproj

echo [2/3] Warte auf %SWAGGER_URL% ...
:wait
curl -s -f -o nul "%SWAGGER_URL%"
if %errorlevel%==0 goto ready
set /a TRIES+=1
if %TRIES% geq 90 goto timeout
timeout /t 2 /nobreak >nul
goto wait

:ready
echo [3/3] Generiere API-Schema...
pushd "%ROOT%LegendsViewer.Frontend\legends-viewer-frontend"
call npm run generate-api-schema
set "RESULT=%errorlevel%"
popd
goto stop

:timeout
echo FEHLER: Backend nach 3 Minuten nicht erreichbar.
set "RESULT=1"

:stop
echo Beende Backend...
taskkill /FI "WINDOWTITLE eq LV-Backend*" /T /F >nul 2>&1

if "%RESULT%"=="0" (echo Fertig.) else (echo Fehlgeschlagen, Code %RESULT%.)
pause
exit /b %RESULT%