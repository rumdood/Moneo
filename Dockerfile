# build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App

COPY . ./
RUN dotnet restore
RUN dotnet publish ./src/Moneo.Hosts/Moneo.Chat.LocalPoller/Moneo.Chat.LocalPoller.csproj -c Release -o out
RUN rm /App/out/appsettings*.json

# runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT [ "dotnet", "Moneo.Chat.LocalPoller.dll" ]