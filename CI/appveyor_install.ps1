Write-Host "Current build configuration is $env:CONFIGURATION"

if ($env:APPVEYOR -and (-not $env:APPVEYOR_PULL_REQUEST_NUMBER) -and $env:CONFIGURATION -eq "Release") {
    Update-AppveyorBuild -Version "$(Get-Content VERSION -Raw)"
}

Write-Host "Current build version is $env:APPVEYOR_BUILD_VERSION"
