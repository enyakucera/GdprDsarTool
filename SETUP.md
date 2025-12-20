# 🛠️ Complete Setup Guide

This guide will walk you through setting up the GDPR DSAR Tool from scratch.

---

## Prerequisites Installation

### 1. Install .NET 8 SDK

**Windows:**
1. Download from https://dotnet.microsoft.com/download/dotnet/8.0
2. Run the installer
3. Verify: Open CMD and run `dotnet --version` (should show 8.0.x)

**macOS:**
```bash
brew install dotnet@8
```

**Linux (Ubuntu):**
```bash
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0
```

### 2. Install SQL Server

**Windows - SQL Server Express:**
1. Download SQL Server 2022 Express from https://www.microsoft.com/sql-server/sql-server-downloads
2. Choose "Basic" installation
3. Note down the server name (usually `localhost\SQLEXPRESS`)
4. Install SQL Server Management Studio (SSMS) from https://aka.ms/ssmsfullsetup

**macOS/Linux - Docker:**
```bash
docker pull mcr.microsoft.com/mssql/server:2022-latest
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Password" \
   -p 1433:1433 --name sql_server_2022 \
   -d mcr.microsoft.com/mssql/server:2022-latest
```

### 3. Install Git
- Windows: https://git-scm.com/download/win
- macOS: `brew install git`
- Linux: `sudo apt-get install git`

---

## Project Setup

### Step 1: Clone the Repository

```bash
# Clone the repo
git clone https://github.com/yourusername/GdprDsarTool.git

# Navigate to project
cd GdprDsarTool/src/GdprDsarTool
```

### Step 2: Create Database

**Option A: Using SSMS (Windows)**
1. Open SSMS
2. Connect to `localhost\SQLEXPRESS`
3. Right-click "Databases" → "New Database"
4. Name: `GdprDsarTool_Dev`
5. Click OK

**Option B: Using T-SQL**
```sql
CREATE DATABASE GdprDsarTool_Dev;
GO
```

**Option C: EF Core will create it automatically**
Skip this step - EF Core migrations will create the database.

### Step 3: Configure Connection String

#### For SQL Express (Windows):

Edit `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=GdprDsarTool_Dev;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

#### For SQL Server with Username/Password:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=GdprDsarTool_Dev;User ID=sa;Password=YourStrong@Password;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

#### For Docker SQL Server:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=GdprDsarTool_Dev;User ID=sa;Password=YourStrong@Password;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### Step 4: Install EF Core Tools

```bash
dotnet tool install --global dotnet-ef
```

Verify installation:
```bash
dotnet ef --version
```

### Step 5: Restore NuGet Packages

```bash
dotnet restore
```

### Step 6: Create and Apply Migrations

```bash
# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migration to database
dotnet ef database update
```

This will:
- Create all tables (Companies, DsarRequests, AdminUsers)
- Create indexes
- Seed demo data (1 company, 1 admin user, 2 sample requests)

### Step 7: Configure Email (Optional)

#### For Gmail SMTP:

1. Enable 2-Factor Authentication on your Google Account
2. Create an App Password: https://myaccount.google.com/apppasswords
3. Edit `appsettings.json`:

```json
{
  "AppSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-16-char-app-password",
    "FromEmail": "noreply@gdprdsar.com"
  }
}
```

#### For Other SMTP Providers:

**SendGrid:**
```json
{
  "SmtpHost": "smtp.sendgrid.net",
  "SmtpPort": 587,
  "SmtpUsername": "apikey",
  "SmtpPassword": "YOUR_SENDGRID_API_KEY"
}
```

**Mailgun:**
```json
{
  "SmtpHost": "smtp.mailgun.org",
  "SmtpPort": 587,
  "SmtpUsername": "postmaster@yourdomain.mailgun.org",
  "SmtpPassword": "YOUR_MAILGUN_PASSWORD"
}
```

---

## Running the Application

### Development Mode

```bash
# Run with hot reload
dotnet watch run
```

OR

```bash
# Standard run
dotnet run
```

The application will start at:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

### First Login

Navigate to: `https://localhost:5001/Admin/Login`

**Default credentials:**
- Email: `admin@democompany.com`
- Password: `Admin123!`

---

## Testing the Application

### 1. Test Public Request Form

1. Go to `https://localhost:5001/Public/SubmitRequest`
2. Fill out the form:
   - Email: `test@example.com`
   - Full Name: `Test User`
   - Request Type: `Access`
   - Message: (optional)
3. Click "Submit Request"
4. You should see a confirmation page with Request ID

### 2. Test Admin Dashboard

1. Login at `https://localhost:5001/Admin/Login`
2. You should see the dashboard with:
   - Statistics cards (Total, Pending, In Progress, Completed)
   - Table with all requests
3. Click "View" on any request
4. Try actions:
   - Generate PDF
   - Update status

### 3. Test PDF Generation

1. In Request Detail, click "Generate PDF Response"
2. Wait for success message
3. Click "Download PDF" to view the generated document
4. PDF should be saved in `wwwroot/pdfs/`

---

## Troubleshooting

### Problem: "Cannot connect to SQL Server"

**Solution:**
1. Verify SQL Server is running:
   ```bash
   # Windows
   services.msc → Look for "SQL Server (SQLEXPRESS)"
   
   # Docker
   docker ps → Check if sql_server_2022 is running
   ```
2. Check connection string in `appsettings.Development.json`
3. Try Windows Authentication: `Trusted_Connection=True`

### Problem: "Migrations fail with permission error"

**Solution:**
1. Make sure your SQL user has `db_owner` role
2. OR use Windows Authentication (easier for local dev)

### Problem: "Emails not sending"

**Solution:**
1. Check SMTP credentials in `appsettings.json`
2. For Gmail, make sure you're using App Password, not account password
3. Check firewall isn't blocking port 587
4. Look at logs in console - email failures are logged but don't break the app

### Problem: "PDF generation fails"

**Solution:**
1. Make sure `wwwroot/pdfs/` folder exists
2. Check folder permissions (needs write access)
3. QuestPDF Community license is auto-configured

### Problem: "Port 5001 already in use"

**Solution:**
Edit `Properties/launchSettings.json` and change ports:
```json
"applicationUrl": "https://localhost:5555;http://localhost:5556"
```

---

## Next Steps

After successful setup:

1. ✅ Explore the codebase
2. ✅ Modify styling in `wwwroot/css/site.css`
3. ✅ Customize PDF template in `Services/PdfService.cs`
4. ✅ Add real data integrations
5. ✅ Deploy to Azure (see README.md)

---

## Development Tools (Recommended)

- **Visual Studio 2022 Community** (Windows) - Full IDE
- **Visual Studio Code** (All platforms) - Lightweight editor
- **JetBrains Rider** (All platforms) - Premium IDE
- **SQL Server Management Studio** (Windows) - Database GUI
- **Azure Data Studio** (All platforms) - Cross-platform DB tool

---

## Useful Commands

```bash
# Restore packages
dotnet restore

# Build project
dotnet build

# Run tests (when added)
dotnet test

# Clean build artifacts
dotnet clean

# Create new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Rollback to specific migration
dotnet ef database update PreviousMigrationName

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script from migrations
dotnet ef migrations script

# Drop database
dotnet ef database drop
```

---

## Project Structure Explained

```
GdprDsarTool/
├── Controllers/           # MVC Controllers (handle HTTP requests)
├── Data/                 # Database context & initialization
├── Models/               # Entity models & ViewModels
├── Services/             # Business logic (PDF, Email)
├── Views/                # Razor views (HTML templates)
├── wwwroot/              # Static files (CSS, JS, generated PDFs)
├── Migrations/           # EF Core database migrations
├── appsettings.json      # Configuration
└── Program.cs            # Application entry point
```

---

## Support

If you run into issues:
1. Check this SETUP.md first
2. Look at console logs for error messages
3. Create a GitHub issue with:
   - Error message
   - Steps to reproduce
   - Your environment (OS, .NET version, SQL Server version)

---

**Happy coding! 🚀**
