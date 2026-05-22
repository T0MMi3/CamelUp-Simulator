FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY CamelUpApi/CamelUpApi.csproj CamelUpApi/
COPY CamelUpSimulator/CamelUpSimulator.csproj CamelUpSimulator/

RUN dotnet restore CamelUpApi/CamelUpApi.csproj

COPY . .

RUN dotnet publish CamelUpApi/CamelUpApi.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "CamelUpApi.dll"]