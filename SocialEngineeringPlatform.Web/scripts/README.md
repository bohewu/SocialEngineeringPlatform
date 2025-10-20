# Admin Password Setup Script (PowerShell 7)

This script sets the production admin password via environment variable so DbInitializer can read it at first run.

- Environment variable used: `AppSettings__AdminUser__Password`
- Optional: set `ASPNETCORE_ENVIRONMENT=Production`

## Requirements
- PowerShell 7 (pwsh)

## Usage

Set for the machine (recommended for Windows Service/IIS app pool):

```powershell
pwsh -File .\scripts\Set-AdminPassword.ps1 -Scope Machine -SetAspNetCoreEnvironment
```

Set for current user (console hosting):

```powershell
pwsh -File .\scripts\Set-AdminPassword.ps1 -Scope User -SetAspNetCoreEnvironment
```

One-time (current process only):

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Production'
$env:AppSettings__AdminUser__Password = (Read-Host "Password")
```

## Verify
- Restart your shell/service and run the app.
- On first startup in Production, DbInitializer seeds the admin user using the password from the environment variable.

## Rotate password
Re-run the script with a new password and restart the app.

## Remove
```powershell
[Environment]::SetEnvironmentVariable('AppSettings__AdminUser__Password', $null, 'Machine')
[Environment]::SetEnvironmentVariable('ASPNETCORE_ENVIRONMENT', $null, 'Machine')
```
