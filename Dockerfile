# =============================================================================
# .NET 10 ASP.NET Core MVC Application - GdprDsarTool
# =============================================================================

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY GdprDsarTool.sln .
COPY src/GdprDsarTool/GdprDsarTool.csproj src/GdprDsarTool/

# Restore dependencies with retry
RUN for i in 1 2 3 4 5; do \
      dotnet restore --disable-parallel && break || sleep 15; \
    done

# Copy everything else and build
COPY . .
WORKDIR /src/src/GdprDsarTool
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Create pdfs directory
RUN mkdir -p wwwroot/pdfs && chmod 777 wwwroot/pdfs

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:8080/ || exit 1

# Set environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Start application
ENTRYPOINT ["dotnet", "GdprDsarTool.dll"]
