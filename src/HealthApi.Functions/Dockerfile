FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/HealthApi.Functions/HealthApi.Functions.csproj", "src/HealthApi.Functions/"]
COPY ["src/HealthApi.EntityFramework/HealthApi.EntityFramework.csproj", "src/HealthApi.EntityFramework/"]
COPY ["src/HealthApi.Domain/HealthApi.Domain.csproj", "src/HealthApi.Domain/"]
RUN dotnet restore "src/HealthApi.Functions/HealthApi.Functions.csproj"
COPY . .
RUN dotnet publish "src/HealthApi.Functions/HealthApi.Functions.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HealthApi.Functions.dll"]
