$ErrorActionPreference = "Stop"
dotnet restore
dotnet build -c Release
Write-Host ""
Write-Host "Build completata." -ForegroundColor Green
