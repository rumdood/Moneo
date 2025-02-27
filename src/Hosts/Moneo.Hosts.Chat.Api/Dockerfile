FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Hosts/Moneo.Hosts.Chat.Api/Moneo.Hosts.Chat.Api.csproj", "src/Hosts/Moneo.Hosts.Chat.Api/"]
RUN dotnet restore "src/Hosts/Moneo.Hosts.Chat.Api/Moneo.Hosts.Chat.Api.csproj"
COPY . .
WORKDIR "/src/src/Hosts/Moneo.Hosts.Chat.Api"
RUN dotnet build "Moneo.Hosts.Chat.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Moneo.Hosts.Chat.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
RUN rm -f /app/publish/appsettings.local.json

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Moneo.Hosts.Chat.Api.dll"]
