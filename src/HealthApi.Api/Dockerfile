FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/HealthApi.Api/HealthApi.Api.csproj", "src/HealthApi.Api/"]
COPY ["src/HealthApi.Domain/HealthApi.Domain.csproj", "src/HealthApi.Domain/"]
COPY ["src/HealthApi.EntityFramework/HealthApi.EntityFramework.csproj", "src/HealthApi.EntityFramework/"]
RUN dotnet restore "src/HealthApi.Api/HealthApi.Api.csproj"
COPY . .
RUN dotnet publish "src/HealthApi.Api/HealthApi.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HealthApi.Api.dll"]
