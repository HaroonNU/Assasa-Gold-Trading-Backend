# Multi-stage build: SDK image compiles/publishes, slim ASP.NET runtime
# image actually serves. No database, no persistent volume — every restart
# starts from the seeded in-memory state by design (see WhatIDid.md).

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/GoldTrading.Domain/GoldTrading.Domain.csproj src/GoldTrading.Domain/
COPY src/GoldTrading.Application/GoldTrading.Application.csproj src/GoldTrading.Application/
COPY src/GoldTrading.Infrastructure/GoldTrading.Infrastructure.csproj src/GoldTrading.Infrastructure/
COPY src/GoldTrading.Api/GoldTrading.Api.csproj src/GoldTrading.Api/
RUN dotnet restore src/GoldTrading.Api/GoldTrading.Api.csproj

COPY src/ src/
RUN dotnet publish src/GoldTrading.Api/GoldTrading.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "GoldTrading.Api.dll"]
