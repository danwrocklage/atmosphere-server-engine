FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Launcher/Launcher.csproj", "Launcher/"]
RUN dotnet restore "Launcher/Launcher.csproj"
COPY . .
WORKDIR "/src/Launcher"
RUN dotnet build "Launcher.csproj" -c Release -o /app/build

FROM build AS publish
ARG ENVIRONMENT=Staging
RUN dotnet publish "Launcher.csproj" -c Release -o /app/publish /p:ServerGarbageCollection=true /p:ConcurrentGarbageCollection=true /p:ACoreEnvironment=$ENVIRONMENT /p:ACoreBuild=CONTAINER

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 6000
ENTRYPOINT ["dotnet", "Launcher.dll"]
