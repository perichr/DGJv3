# called by msbuild

$isAppveyor = if ($env:APPVEYOR -eq $null) { "false" } else { $env:APPVEYOR }
$githash = git rev-parse --verify HEAD
$githashshort = $githash.Substring(0, 8)
$buildversion = if ($env:APPVEYOR_BUILD_VERSION -eq $null) { "branch:$(git rev-parse --abbrev-ref HEAD)-$(Get-Date -Format 'yyyy/MM/dd HH:mm:ss')" } else { $env:APPVEYOR_BUILD_VERSION }

(Get-Content .\DGJv3\Data\BuildInfo.txt).Replace('[APPVEYOR]', $isAppveyor.ToLower()).Replace('[VERSION]', $buildversion).Replace('[GIT_HASH]', $githash).Replace('[GIT_HASH_S]', $githashshort) | Set-Content .\DGJv3\Data\BuildInfo.cs -Encoding UTF8
