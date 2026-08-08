FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Innkeep2.Database/Innkeep2.Database.csproj", "Innkeep2.Database/"]
RUN dotnet restore "Innkeep2.Database/Innkeep2.Database.csproj"
COPY . .
WORKDIR "/src/Innkeep2.Database"
RUN dotnet build "./Innkeep2.Database.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Innkeep2.Database.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Innkeep2.Database.dll"]
