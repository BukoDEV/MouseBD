@echo off
echo MouseBD Server - Touchpad dla Android
echo =======================================
echo.
echo Budowanie...
dotnet build MouseBD.Server.csproj -c Release -o bin\server
echo.
echo Uruchamianie serwera...
start bin\server\MouseBD.Server.exe
echo.
echo Serwer uruchomiony w zasobniku systemowym!
echo Kliknij dwukrotnie ikonke w zasobniku aby zobaczyc adres IP.
