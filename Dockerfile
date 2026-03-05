# ── Stage 1: Build ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore (layer caching)
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/PAMS.Domain/PAMS.Domain.csproj             src/PAMS.Domain/
COPY src/PAMS.Application/PAMS.Application.csproj   src/PAMS.Application/
COPY src/PAMS.Infrastructure/PAMS.Infrastructure.csproj src/PAMS.Infrastructure/
COPY src/PAMS.API/PAMS.API.csproj                   src/PAMS.API/

RUN dotnet restore src/PAMS.API/PAMS.API.csproj --os linux --arch x64

# Copy everything and publish
COPY src/ src/
RUN dotnet publish src/PAMS.API/PAMS.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    --os linux \
    --arch x64 \
    -p:UseAppHost=false

# ── Stage 2: Runtime ───────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app

# Use the built-in non-root 'app' user (included since .NET 8)
USER app

COPY --chown=app:app --from=build /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "PAMS.API.dll"]
