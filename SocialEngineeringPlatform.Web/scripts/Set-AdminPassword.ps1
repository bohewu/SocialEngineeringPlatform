param(
    [ValidateSet('Process','User','Machine')] [string]$Scope = 'User',
    [switch]$SetAspNetCoreEnvironment
)

$ErrorActionPreference = 'Stop'

$envVarName = 'AppSettings__AdminUser__Password'

Write-Host "This will set the admin password environment variable: $envVarName" -ForegroundColor Yellow
Write-Host "Scope: $Scope" -ForegroundColor Yellow

function Read-PlainFromSecure([securestring]$sec) {
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec)
    try { [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) } finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
}

# Prompt until passwords match
while ($true) {
    $s1 = Read-Host "Enter admin password" -AsSecureString
    $s2 = Read-Host "Confirm admin password" -AsSecureString

    $p1 = Read-PlainFromSecure $s1
    $p2 = Read-PlainFromSecure $s2

    if ($p1 -ne $p2) {
        Write-Host "Passwords do not match. Try again." -ForegroundColor Red
        continue
    }

    if ([string]::IsNullOrWhiteSpace($p1)) {
        Write-Host "Password cannot be empty. Try again." -ForegroundColor Red
        continue
    }

    break
}

# Set env var at requested scope
[Environment]::SetEnvironmentVariable($envVarName, $p1, $Scope)

if ($SetAspNetCoreEnvironment) {
    [Environment]::SetEnvironmentVariable('ASPNETCORE_ENVIRONMENT', 'Production', $Scope)
}

# Clear sensitive data from memory
$s1 = $null; $s2 = $null; $p1 = $null; $p2 = $null
[GC]::Collect(); [GC]::WaitForPendingFinalizers()

Write-Host "Environment variable '$envVarName' set at scope: $Scope" -ForegroundColor Green
if ($SetAspNetCoreEnvironment) {
    Write-Host "ASPNETCORE_ENVIRONMENT=Production set at scope: $Scope" -ForegroundColor Green
}
Write-Host "Restart your shell/service so the environment changes take effect." -ForegroundColor Yellow
