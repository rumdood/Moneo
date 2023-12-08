#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/azure-functions/dotnet:4 AS base
WORKDIR /home/site/wwwroot
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["src/Moneo.Hosts/Moneo.Functions.Isolated/Moneo.Functions.Isolated.csproj", "src/Moneo.Hosts/Moneo.Functions.Isolated/"]
RUN dotnet restore "./src/Moneo.Hosts/Moneo.Functions.Isolated/./Moneo.Functions.Isolated.csproj"
COPY . .
WORKDIR "/src/src/Moneo.Hosts/Moneo.Functions.Isolated"
RUN dotnet build "./Moneo.Functions.Isolated.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Moneo.Functions.Isolated.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true