FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID

WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Innkeep2.Cloud/Innkeep2.Cloud.csproj", "Innkeep2.Cloud/"]
COPY ["Innkeep2.Credentials/Innkeep2.Credentials.csproj", "Innkeep2.Credentials/"]
COPY ["Innkeep2.Requests/Innkeep2.Requests.csproj", "Innkeep2.Requests/"]
COPY ["Innkeep2.Models/Innkeep2.Models.csproj", "Innkeep2.Models/"]
COPY ["Innkeep2.Services/Innkeep2.Services.csproj", "Innkeep2.Services/"]
COPY ["Innkeep2.Database/Innkeep2.Database.csproj", "Innkeep2.Database/"]
RUN dotnet restore "Innkeep2.Cloud/Innkeep2.Cloud.csproj"
COPY . .
WORKDIR "/src/Innkeep2.Cloud"
RUN dotnet build "./Innkeep2.Cloud.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Innkeep2.Cloud.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

USER root
RUN mkdir -p /app/db && chown $APP_UID /app/db
RUN mkdir -p /app/dataprotection-keys && chown $APP_UID /app/dataprotection-keys

USER $APP_UID

ENTRYPOINT ["dotnet", "Innkeep2.Cloud.dll"]
