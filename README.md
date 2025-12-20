# GDPR DSAR Automation Tool

🚀 **A simple SaaS prototype for automating GDPR Data Subject Access Requests (DSAR)**

Built with ASP.NET Core 8, MS SQL Server, and Bootstrap 5.

---

## 📋 Features

- ✅ **Public Request Form** - Simple web form for data subjects to submit GDPR requests
- ✅ **Admin Dashboard** - Centralized panel to manage all requests
- ✅ **PDF Generation** - One-click PDF generation with QuestPDF
- ✅ **Email Notifications** - Automatic emails to requesters and admins
- ✅ **Status Tracking** - Track request lifecycle (Pending → In Progress → Completed)

---

## 🛠️ Tech Stack

- **Backend:** ASP.NET Core 8 MVC
- **Database:** MS SQL Server (SQL Express for local, Azure SQL for production)
- **ORM:** Entity Framework Core 8
- **PDF:** QuestPDF
- **Email:** MailKit (SMTP)
- **Auth:** Simple session-based authentication (BCrypt password hashing)
- **Frontend:** Bootstrap 5, jQuery

---

## 🚀 Quick Start

### Prerequisites

- .NET 8 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- SQL Server Express or Azure SQL Database
- SQL Server Management Studio (SSMS) - optional but recommended

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/GdprDsarTool.git
cd GdprDsarTool/src/GdprDsarTool
```

### 2. Configure Database Connection

Edit `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=GdprDsarTool_Dev;User ID=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

Replace `YOUR_USER` and `YOUR_PASSWORD` with your SQL Server credentials.

### 3. Configure Email (Optional)

Edit `appsettings.json` to configure SMTP settings:

```json
{
  "AppSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "FromEmail": "noreply@gdprdsar.com"
  }
}
```

**Note:** For Gmail, you need to create an [App Password](https://support.google.com/accounts/answer/185833).

### 4. Run Database Migrations

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

This will:
- Create the database
- Create tables (Companies, DsarRequests, AdminUsers)
- Seed demo data

### 5. Run the Application

```bash
dotnet run
```

Navigate to: `https://localhost:5001` (or the URL shown in console)

### 6. Login to Admin Panel

Default credentials (seeded automatically):

- **Email:** `admin@democompany.com`
- **Password:** `Admin123!`

---

## 📁 Project Structure

```
GdprDsarTool/
├── Controllers/
│   ├── HomeController.cs          # Landing page
│   ├── PublicController.cs        # Public DSAR form
│   └── AdminController.cs         # Admin dashboard
├── Data/
│   ├── AppDbContext.cs            # EF Core DbContext
│   └── DbInitializer.cs           # Seed data
├── Models/
│   ├── Company.cs
│   ├── DsarRequest.cs
│   ├── AdminUser.cs
│   └── ViewModels/
├── Services/
│   ├── PdfService.cs              # QuestPDF implementation
│   └── EmailService.cs            # MailKit implementation
├── Views/
│   ├── Home/
│   ├── Public/
│   ├── Admin/
│   └── Shared/
└── wwwroot/
    ├── css/
    ├── js/
    └── pdfs/                      # Generated PDFs
```

---

## 🗄️ Database Schema

### Companies
- Id (GUID)
- Name
- Email
- CreatedAt

### DsarRequests
- Id (GUID)
- CompanyId (FK)
- RequesterEmail
- RequesterName
- RequestType (Access/Delete/Rectify)
- Status (Pending/InProgress/Completed/Rejected)
- ResponsePdfUrl
- SubmittedAt
- CompletedAt

### AdminUsers
- Id (GUID)
- Email
- PasswordHash (BCrypt)
- CompanyId (FK)
- CreatedAt

---

## 🔐 Security Notes

⚠️ **This is a PROTOTYPE** - not production-ready. Security improvements needed:

- [ ] Replace session-based auth with proper Identity/JWT
- [ ] Add HTTPS enforcement
- [ ] Implement CSRF protection on all forms
- [ ] Add rate limiting
- [ ] Implement proper multi-tenancy isolation
- [ ] Add audit logging
- [ ] Secure file storage (Azure Blob Storage instead of wwwroot)
- [ ] Add input sanitization

---

## 🚢 Deployment to Azure

### Option 1: Azure App Service + Azure SQL

1. **Create Azure SQL Database:**
```bash
az sql server create --name gdpr-dsar-sql --resource-group GdprDsarToolRG --location westeurope --admin-user sqladmin --admin-password YourSecurePassword123!
az sql db create --resource-group GdprDsarToolRG --server gdpr-dsar-sql --name GdprDsarTool --service-objective Basic
```

2. **Create App Service:**
```bash
az appservice plan create --name GdprDsarToolPlan --resource-group GdprDsarToolRG --sku B1 --is-linux
az webapp create --resource-group GdprDsarToolRG --plan GdprDsarToolPlan --name gdpr-dsar-tool --runtime "DOTNETCORE:8.0"
```

3. **Configure Connection String in Azure:**
```bash
az webapp config connection-string set --resource-group GdprDsarToolRG --name gdpr-dsar-tool --connection-string-type SQLAzure --settings DefaultConnection="Server=tcp:gdpr-dsar-sql.database.windows.net,1433;Database=GdprDsarTool;User ID=sqladmin;Password=YourSecurePassword123!;"
```

4. **Deploy:**
```bash
dotnet publish -c Release
cd bin/Release/net8.0/publish
zip -r deploy.zip .
az webapp deployment source config-zip --resource-group GdprDsarToolRG --name gdpr-dsar-tool --src deploy.zip
```

---

## 📊 Roadmap

### Phase 1: MVP (Current) ✅
- [x] Basic DSAR form
- [x] Admin dashboard
- [x] PDF generation
- [x] Email notifications

### Phase 2: Beta Improvements
- [ ] Real data integrations (API connectors)
- [ ] Advanced PDF templates
- [ ] Multi-company support (SaaS mode)
- [ ] Stripe payment integration
- [ ] Analytics dashboard

### Phase 3: Production
- [ ] AI-powered data discovery
- [ ] GDPR compliance scoring
- [ ] Automated audit reports
- [ ] Mobile app

---

## 🤝 Contributing

This is a learning/prototype project. Feel free to fork and experiment!

---

## 📝 License

MIT License - See LICENSE file for details

---

## 💬 Support

For questions or feedback:
- GitHub Issues: [Create an issue](https://github.com/yourusername/GdprDsarTool/issues)
- Email: your-email@example.com

---

**Built with ❤️ as part of EU Compliance SaaS exploration**
