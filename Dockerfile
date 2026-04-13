# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first (layer-cache friendly)
COPY TaskManagement.sln ./
COPY src/TaskManagement.Domain/TaskManagement.Domain.csproj                       src/TaskManagement.Domain/
COPY src/TaskManagement.Application/TaskManagement.Application.csproj             src/TaskManagement.Application/
COPY src/TaskManagement.Infrastructure/TaskManagement.Infrastructure.csproj       src/TaskManagement.Infrastructure/
COPY src/TaskManagement.API/TaskManagement.API.csproj                             src/TaskManagement.API/

RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish src/TaskManagement.API/TaskManagement.API.csproj \
    -c Release -o /app/publish --no-restore

# ── Runtime stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TaskManagement.API.dll"]
