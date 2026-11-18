FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Domain/Domain.csproj Domain/
COPY RedLightGreenLight/RedLightGreenLight.csproj RedLightGreenLight/
RUN dotnet restore RedLightGreenLight/RedLightGreenLight.csproj

COPY Domain/ Domain/
COPY RedLightGreenLight/ RedLightGreenLight/
RUN dotnet publish RedLightGreenLight/RedLightGreenLight.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "RedLightGreenLight.dll"]
