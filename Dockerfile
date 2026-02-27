# ── Stage 1: Build ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution, props files, and restore first (layer caching)
COPY Directory.Build.props Directory.Packages.props PAMS.slnx global.json ./
COPY src/PAMS.Domain/PAMS.Domain.csproj             src/PAMS.Domain/
COPY src/PAMS.Application/PAMS.Application.csproj   src/PAMS.Application/
COPY src/PAMS.Infrastructure/PAMS.Infrastructure.csproj src/PAMS.Infrastructure/
COPY src/PAMS.API/PAMS.API.csproj                   src/PAMS.API/

RUN dotnet restore PAMS.slnx

# Copy everything and publish
COPY src/ src/
RUN dotnet publish src/PAMS.API/PAMS.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ───────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "PAMS.API.dll"]
