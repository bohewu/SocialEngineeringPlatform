# Social Engineering Platform - Setup Guide

## Prerequisites

- .NET 9.0 SDK
- PowerShell 7 (for production password setup)
- One of the following databases:
  - **SQLite** (default, no installation needed)
  - **SQL Server** or **SQL Server Express**
  - **PostgreSQL**

---

## Quick Start (Development with SQLite)

### 1. Clone and restore packages

```bash
git clone <your-repo-url>
cd SocialEngineeringPlatform.Web
dotnet restore
```

### 2. Set admin password (Development)

```bash
dotnet user-secrets set "AppSettings:AdminUser:Password" "YourDevPassword123!"
```

### 3. Create initial migration and database

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 4. Run the application

```bash
dotnet run
```

The app will:
- Use SQLite by default (`DatabaseProvider: "Sqlite"`)
- Create database at `bin/Debug/net9.0/SocialEngineeringDB.db`
- Seed admin user with email from `appsettings.Development.json`

---

## Production Setup

### 1. Choose your database

Edit `appsettings.json` and set `DatabaseProvider`:

```json
{
  "DatabaseProvider": "Sqlite",  // or "SqlServer" or "Postgres"
  "ConnectionStrings": {
    "SqliteConnection": "Data Source=SocialEngineeringDB.db",
    "SqlServerConnection": "Server=...;Database=SocialEngineeringDB;...",
    "PostgresConnection": "Host=...;Database=SocialEngineeringDB;..."
  }
}
```

### 2. Set admin password via environment variable

**For Windows Service or IIS (Machine scope):**

```powershell
pwsh -File .\scripts\Set-AdminPassword.ps1 -Scope Machine -SetAspNetCoreEnvironment
```

**For console hosting (User scope):**

```powershell
pwsh -File .\scripts\Set-AdminPassword.ps1 -Scope User -SetAspNetCoreEnvironment
```

This sets:
- `AppSettings__AdminUser__Password` = (your password)
- `ASPNETCORE_ENVIRONMENT` = `Production`

### 3. Create migrations and database

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

**Important:** Migrations are provider-specific and not included in Git. Each deployment environment must generate its own migrations.

### 4. Publish and deploy

```bash
dotnet publish -c Release -o ./publish
```

Copy the `./publish` folder to your server and run:

```bash
cd /path/to/publish
dotnet SocialEngineeringPlatform.Web.dll
```

---

## Switching Database Providers

### From SQLite to SQL Server

1. Edit `appsettings.json`:
   ```json
   "DatabaseProvider": "SqlServer"
   ```

2. Delete old migrations:
   ```bash
   Remove-Item -Path .\Migrations\* -Recurse -Force
   ```

3. Create new migrations:
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

### From SQLite to PostgreSQL

1. Edit `appsettings.json`:
   ```json
   "DatabaseProvider": "Postgres"
   ```

2. Ensure PostgreSQL is running and database exists:
   ```bash
   psql -U postgres -c "CREATE DATABASE SocialEngineeringDB;"
   ```

3. Delete old migrations and create new ones:
   ```bash
   Remove-Item -Path .\Migrations\* -Recurse -Force
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

---

## Verification

### Health Check

Navigate to:
- `http://localhost:5000/health` (or your configured URL)

Expected response: HTTP 200 with `Healthy` status.

### Check Database Provider

On startup, the console will show:

```
Using Database Provider: Sqlite
SQLite Database Path: C:\...\bin\Debug\net9.0\SocialEngineeringDB.db
```

---

## Troubleshooting

### Migrations already exist for a different provider

**Solution:** Delete the `Migrations/` folder and recreate:

```bash
Remove-Item -Path .\Migrations\* -Recurse -Force
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Admin user not created

**Symptoms:** Cannot log in after first run.

**Solution:** Verify password is set:

**Development:**
```bash
dotnet user-secrets list
```

**Production:**
```powershell
[Environment]::GetEnvironmentVariable('AppSettings__AdminUser__Password', 'Machine')
```

### Database connection fails

**SQL Server:**
- Ensure SQL Server is running
- Check connection string (server name, authentication)
- Verify firewall allows connections

**PostgreSQL:**
- Ensure PostgreSQL is running: `sudo systemctl status postgresql`
- Check `pg_hba.conf` for connection permissions
- Verify database exists: `psql -U postgres -l`

---

## Additional Documentation

- **SQLite Deployment:** See `SQLITE_DEPLOYMENT_GUIDE.md`
- **Multi-Database Setup:** See `MULTI_DATABASE_GUIDE.md`
- **Implementation Details:** See `IMPLEMENTATION_SUMMARY.md`
- **Admin Password Script:** See `scripts/README.md`

---

## Default Admin Credentials

After first run, log in with:

- **Email:** From `appsettings.json` → `AppSettings:AdminUser:Email`
- **Password:** From user-secrets (dev) or environment variable (prod)

---

## Security Notes

- ⚠️ Change the default admin password immediately after first login
- ⚠️ Never commit `appsettings.Production.json` with real passwords
- ⚠️ Use environment variables or Azure Key Vault for production secrets
- ⚠️ Keep the `Migrations/` folder out of Git (provider-specific)
