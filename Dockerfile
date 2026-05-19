FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Motor.Claim.WebApi.sln", "./"]
COPY ["Motor.Claim.Domain/Motor.Claim.Domain.csproj", "Motor.Claim.Domain/"]
COPY ["Motor.Claim.Application/Motor.Claim.Application.csproj", "Motor.Claim.Application/"]
COPY ["Motor.Claim.Infrastructure.Persistence/Motor.Claim.Infrastructure.Persistence.csproj", "Motor.Claim.Infrastructure.Persistence/"]
COPY ["Motor.Claim.Infrastructure.Shared/Motor.Claim.Infrastructure.Shared.csproj", "Motor.Claim.Infrastructure.Shared/"]
COPY ["Motor.Claim.WebApi/Motor.Claim.WebApi.csproj", "Motor.Claim.WebApi/"]

RUN dotnet restore "Motor.Claim.WebApi.sln" --disable-parallel

COPY . .
RUN dotnet publish "Motor.Claim.WebApi/Motor.Claim.WebApi.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet Motor.Claim.WebApi.dll"]
