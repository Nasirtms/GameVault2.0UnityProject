# WebGL Deployment Script
# This script deletes the old WebGL folder on the server and uploads the new one

# ===== CONFIGURATION =====
$SERVER_IP = "3.231.201.150"
$SERVER_PORT = "22"  # SSH port (default is 22)
$SERVER_USER = "ubuntu"  # Change this to your server username
$SSH_KEY_PATH = "D:\Unity TMS Dev\ServerData\aws-games-key.pem"  # SSH key file
$LOCAL_WEBGL_PATH = "D:\Unity TMS Dev\ServerData\WebGL"
$REMOTE_WEBGL_PATH = "/var/www/html/casino"  # Change this to your server's WebGL path

# ===== SCRIPT =====
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "WebGL Deployment Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Check if local WebGL folder exists
if (-Not (Test-Path $LOCAL_WEBGL_PATH)) {
    Write-Host "ERROR: Local WebGL folder not found at: $LOCAL_WEBGL_PATH" -ForegroundColor Red
    exit 1
}

Write-Host "Local WebGL folder: $LOCAL_WEBGL_PATH" -ForegroundColor Green
Write-Host "Remote server: $SERVER_USER@$SERVER_IP" -ForegroundColor Green
Write-Host "Remote path: $REMOTE_WEBGL_PATH/WebGL" -ForegroundColor Green
Write-Host ""

# Step 1: Delete old WebGL folder on server
Write-Host "[Step 1/4] Deleting old WebGL folder on server..." -ForegroundColor Yellow
ssh -i "$SSH_KEY_PATH" -p $SERVER_PORT -o StrictHostKeyChecking=no "$SERVER_USER@$SERVER_IP" "sudo rm -rf $REMOTE_WEBGL_PATH/WebGL"

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Old WebGL folder deleted successfully" -ForegroundColor Green
} else {
    Write-Host "[INFO] Failed to delete old WebGL folder (this might be OK if folder doesn't exist)" -ForegroundColor Yellow
}

Write-Host ""

# Step 2: Upload to temporary location
Write-Host "[Step 2/4] Uploading WebGL folder to server (temporary location)..." -ForegroundColor Yellow
scp -i "$SSH_KEY_PATH" -P $SERVER_PORT -o StrictHostKeyChecking=no -r "$LOCAL_WEBGL_PATH" "$SERVER_USER@$SERVER_IP`:~/WebGL"

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Upload completed" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Failed to upload WebGL folder" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 3: Create parent directory and move to final location
Write-Host "[Step 3/4] Moving WebGL folder to final location..." -ForegroundColor Yellow
ssh -i "$SSH_KEY_PATH" -p $SERVER_PORT -o StrictHostKeyChecking=no "$SERVER_USER@$SERVER_IP" "sudo mkdir -p $REMOTE_WEBGL_PATH && sudo mv ~/WebGL $REMOTE_WEBGL_PATH/"

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] WebGL folder moved successfully" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Failed to move WebGL folder" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 4: Set proper permissions (optional)
Write-Host "[Step 4/4] Setting proper permissions..." -ForegroundColor Yellow
ssh -i "$SSH_KEY_PATH" -p $SERVER_PORT -o StrictHostKeyChecking=no "$SERVER_USER@$SERVER_IP" "sudo chown -R ubuntu:ubuntu $REMOTE_WEBGL_PATH/WebGL && sudo chmod -R 755 $REMOTE_WEBGL_PATH/WebGL"

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Permissions set successfully" -ForegroundColor Green
} else {
    Write-Host "[INFO] Failed to set permissions (may need to be done manually)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "DEPLOYMENT COMPLETED!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Your WebGL build is now available at:" -ForegroundColor Cyan
Write-Host "http://$SERVER_IP`:3000/WebGL/" -ForegroundColor White
Write-Host ""

