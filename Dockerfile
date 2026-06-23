# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar csproj y restaurar (capa cacheable).
COPY src/Logistics.Domain/Logistics.Domain.csproj src/Logistics.Domain/
COPY src/Logistics.Application/Logistics.Application.csproj src/Logistics.Application/
COPY src/Logistics.Infrastructure/Logistics.Infrastructure.csproj src/Logistics.Infrastructure/
COPY src/Logistics.Api/Logistics.API.csproj src/Logistics.Api/
RUN dotnet restore src/Logistics.Api/Logistics.API.csproj

# Copiar el resto y publicar.
COPY . .
RUN dotnet publish src/Logistics.Api/Logistics.API.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Logistics.API.dll"]
