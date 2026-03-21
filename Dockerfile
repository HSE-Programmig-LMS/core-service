# -------- build stage --------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution + project files first for better layer caching
COPY CoreService.slnx ./

COPY src/CoreService.Api/CoreService.Api.csproj src/CoreService.Api/
COPY src/CoreService.Application/CoreService.Application.csproj src/CoreService.Application/
COPY src/CoreService.Domain/CoreService.Domain.csproj src/CoreService.Domain/
COPY src/CoreService.Infrastructure/CoreService.Infrastructure.csproj src/CoreService.Infrastructure/

# Restore
RUN dotnet restore src/CoreService.Api/CoreService.Api.csproj

# Copy everything and publish
COPY . .
RUN dotnet publish src/CoreService.Api/CoreService.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# -------- runtime stage --------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# App listens on 8080 inside container
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "CoreService.Api.dll"]