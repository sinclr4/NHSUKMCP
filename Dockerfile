# Multi-stage build for NHS UK MCP Server
# Using .NET 9 (preview) images; adjust tag if GA available

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
# Non-root user for security
RUN useradd -m appuser

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore NHSUKMCP.csproj \
 && dotnet publish NHSUKMCP.csproj -c Release -o /out /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /out .
USER appuser
# Expose optional health port
EXPOSE 8080
# Environment variables documented in AZURE_SETUP.md
ENV DOTNET_ENVIRONMENT=Production \
    LOG_LEVEL=Information
ENTRYPOINT ["dotnet", "NHSUKMCP.dll"]
