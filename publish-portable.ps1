$ErrorActionPreference = "Stop"

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true

$publish = Join-Path $PSScriptRoot "bin\Release\net8.0-windows\win-x64\publish"
Write-Host ""
Write-Host "EXE creato in:" -ForegroundColor Green
Write-Host $publish
