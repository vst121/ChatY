# Deployment Guide

This guide covers deploying ChatY to production environments. ChatY is built with .NET 10 and can be deployed to various platforms including Azure, IIS, and Docker containers.

## Prerequisites

Before deploying, ensure you have:

- .NET 10 SDK installed on your development machine
- Access to a production database (SQL Server, Azure SQL)
- Azure account (for Azure deployments)
- SSL certificate for HTTPS

## Environment Configuration

### Production App Settings

Create `appsettings.Production.json` in the `src/ChatY.Server` directory:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-sql-server;Database=ChatY;User Id=your-user;Password=your-password;MultipleActiveResultSets=true;Encrypt=true;TrustServerCertificate=false;"
  },
  "Azure": {
    "Storage": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=yourstorageaccount;AccountKey=yourkey;EndpointSuffix=core.windows.net"
    },
    "KeyVault": {
      "Url": "https://your-keyvault.vault.azure.net/"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

### Environment Variables

Set these environment variables in your production environment:

- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=https://+:443;http://+:80`
- `ConnectionStrings__DefaultConnection` - Database connection string
- `Azure__Storage__ConnectionString` - Azure Storage connection string
- `Azure__KeyVault__Url` - Azure Key Vault URL

## Azure App Service Deployment

### Option 1: Azure Portal

1. **Create Azure Resources**:

   - Azure App Service
   - Azure SQL Database
   - Azure Storage Account
   - Azure Key Vault (optional)

2. **Configure App Service**:

   - Runtime stack: .NET 10
   - Operating System: Windows or Linux
   - Publish: Code

3. **Database Setup**:

   - Create Azure SQL Database
   - Run migrations: `dotnet ef database update`

4. **Deploy Application**:
   - Use Azure DevOps, GitHub Actions, or manual publish
   - Set connection strings in App Service Configuration

### Option 2: Azure CLI

```bash
# Create resource group
az group create --name ChatYResourceGroup --location eastus

# Create App Service Plan
az appservice plan create --name ChatYPlan --resource-group ChatYResourceGroup --sku B1

# Create Web App
az webapp create --name chaty-app --resource-group ChatYResourceGroup --plan ChatYPlan --runtime "DOTNET|10.0"

# Configure connection strings
az webapp config connection-string set --name chaty-app --resource-group ChatYResourceGroup --connection-string-type SQLAzure --settings DefaultConnection="Server=tcp:chaty-server.database.windows.net,1433;Initial Catalog=ChatY;Persist Security Info=False;User ID=admin;Password=password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# Deploy
az webapp deployment source config --name chaty-app --resource-group ChatYResourceGroup --repo-url https://github.com/your-repo/ChatY.git --branch main --manual-integration
```

## IIS Deployment (Windows Server)

### Prerequisites

- Windows Server 2019/2022
- IIS with ASP.NET Core Module
- SQL Server

### Deployment Steps

1. **Install .NET Hosting Bundle**:

   ```powershell
   # Download and install .NET 10 Hosting Bundle
   # From: https://dotnet.microsoft.com/download/dotnet/10.0
   ```

2. **Configure IIS**:

   - Install IIS features
   - Install ASP.NET Core Module
   - Create application pool with No Managed Code

3. **Publish Application**:

   ```bash
   dotnet publish src/ChatY.Server/ChatY.Server.csproj -c Release -o ./publish
   ```

4. **Deploy to IIS**:

   - Copy published files to IIS site directory
   - Configure web.config
   - Set permissions for application pool user

5. **Database Migration**:
   ```bash
   dotnet ef database update --project src/ChatY.Infrastructure --startup-project src/ChatY.Server
   ```

## Docker Deployment

### Dockerfile

Create `Dockerfile` in the root directory:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/ChatY.Server/ChatY.Server.csproj", "src/ChatY.Server/"]
COPY ["src/ChatY.Core/ChatY.Core.csproj", "src/ChatY.Core/"]
COPY ["src/ChatY.Infrastructure/ChatY.Infrastructure.csproj", "src/ChatY.Infrastructure/"]
COPY ["src/ChatY.Services/ChatY.Services.csproj", "src/ChatY.Services/"]
COPY ["src/ChatY.Shared/ChatY.Shared.csproj", "src/ChatY.Shared/"]
RUN dotnet restore "src/ChatY.Server/ChatY.Server.csproj"
COPY . .
WORKDIR "/src/src/ChatY.Server"
RUN dotnet build "ChatY.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChatY.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatY.Server.dll"]
```

### Docker Compose

Create `docker-compose.yml`:

```yaml
version: "3.8"
services:
  chaty:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:80"
      - "8443:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=ChatY;User Id=sa;Password=YourPassword123!
    depends_on:
      - sqlserver

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourPassword123!
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql

volumes:
  sqlserver_data:
```

### Run with Docker

```bash
# Build and run
docker-compose up -d

# Run migrations
docker-compose exec chaty dotnet ef database update --project src/ChatY.Infrastructure --startup-project src/ChatY.Server
```

## Azure Container Instances

Deploy to Azure Container Instances:

```bash
# Build and push to Azure Container Registry
az acr build --registry yourregistry --image chaty:latest .

# Deploy to ACI
az container create --resource-group ChatYResourceGroup --name chaty-container --image yourregistry.azurecr.io/chaty:latest --dns-name-label chaty-app --ports 80 443
```

## Load Balancing and Scaling

### Azure Front Door

For global distribution:

1. Create Azure Front Door
2. Configure backend pools with multiple App Services
3. Set up routing rules
4. Configure custom domain and SSL

### Azure Application Gateway

For advanced load balancing:

1. Create Application Gateway
2. Configure listeners and rules
3. Set up health probes
4. Enable WAF for security

## Monitoring and Logging

### Application Insights

1. Add Application Insights to your Azure resources
2. Configure instrumentation in `Program.cs`
3. Monitor performance, errors, and usage

### Azure Monitor

- Set up alerts for key metrics
- Configure log analytics
- Monitor resource usage

## Security Considerations

### SSL/TLS

- Always use HTTPS in production
- Configure SSL certificates
- Enable HSTS headers

### Authentication

- Implement proper authentication (Azure AD, Identity Server)
- Use secure password policies
- Enable MFA

### Data Protection

- Encrypt sensitive data at rest
- Use Azure Key Vault for secrets
- Implement proper access controls

## Backup and Recovery

### Database Backups

- Configure automated backups in Azure SQL
- Set retention policies
- Test restore procedures

### Application Backups

- Use Azure Backup for App Services
- Backup configuration and secrets
- Document disaster recovery procedures

## Performance Optimization

### Caching

- Implement Redis cache for session data
- Use Azure CDN for static assets
- Configure output caching

### Database Optimization

- Monitor query performance
- Add appropriate indexes
- Consider read replicas for high traffic

## Troubleshooting

### Common Issues

1. **Connection Timeouts**: Check database connectivity and firewall rules
2. **Memory Issues**: Monitor App Service memory usage and scale up if needed
3. **SSL Errors**: Verify certificate configuration and domain settings

### Logs

- Check Application Insights for errors
- Review IIS logs for deployment issues
- Monitor Azure Monitor for resource issues

## Post-Deployment Checklist

- [ ] Application is accessible via HTTPS
- [ ] Database connections are working
- [ ] File uploads to Azure Storage work
- [ ] Real-time features (SignalR) function
- [ ] Authentication is configured
- [ ] SSL certificate is valid
- [ ] Monitoring is set up
- [ ] Backups are configured
- [ ] Performance is acceptable
