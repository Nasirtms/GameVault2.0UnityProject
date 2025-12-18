
@echo off
REM WebGL Deployment Script (Batch Version)
REM This script deletes the old WebGL folder on the server and uploads the new one

REM ===== CONFIGURATION =====
set SERVER_IP=3.231.201.150
set SERVER_PORT=22
set SERVER_USER=ubuntu
set LOCAL_WEBGL_PATH=D:\Unity TMS Dev\ServerData\WebGL
set REMOTE_WEBGL_PATH=/var/www/html/WebGL

REM ===== SCRIPT =====
echo =====================================
echo WebGL Deployment Script
echo =====================================
echo.

REM Check if local WebGL folder exists
if not exist "%LOCAL_WEBGL_PATH%" (
    echo ERROR: Local WebGL folder not found at: %LOCAL_WEBGL_PATH%
    exit /b 1
)

echo Local WebGL folder: %LOCAL_WEBGL_PATH%
echo Remote server: %SERVER_USER%@%SERVER_IP%
echo Remote path: %REMOTE_WEBGL_PATH%
echo.

REM Step 1: Delete old WebGL folder on server
echo [Step 1/2] Deleting old WebGL folder on server...
ssh -p %SERVER_PORT% %SERVER_USER%@%SERVER_IP% "sudo rm -rf %REMOTE_WEBGL_PATH%"
echo Old WebGL folder deleted
echo.

REM Step 2: Upload new WebGL folder
echo [Step 2/2] Uploading new WebGL folder to server...
scp -P %SERVER_PORT% -r "%LOCAL_WEBGL_PATH%" %SERVER_USER%@%SERVER_IP%:%REMOTE_WEBGL_PATH%
echo.

REM Step 3: Set proper permissions
echo [Step 3/3] Setting proper permissions...
ssh -p %SERVER_PORT% %SERVER_USER%@%SERVER_IP% "sudo chown -R www-data:www-data %REMOTE_WEBGL_PATH% && sudo chmod -R 755 %REMOTE_WEBGL_PATH%"
echo.

echo =====================================
echo DEPLOYMENT COMPLETED!
echo =====================================
echo.
echo Your WebGL build is now available at:
echo http://%SERVER_IP%:5036/WebGL/
echo.

pause

