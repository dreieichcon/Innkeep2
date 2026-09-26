FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081



FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Innkeep2.Server/Innkeep2.Server.csproj", "Innkeep2.Server/"]
RUN dotnet restore "Innkeep2.Server/Innkeep2.Server.csproj"
COPY . .
WORKDIR "/src/Innkeep2.Server"
RUN dotnet build "./Innkeep2.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Innkeep2.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
ENV LC_ALL=de-DE.UTF-8
ENV LANG=de-DE.UTF-8
WORKDIR /app
COPY --from=publish /app/publish .

USER root
RUN mkdir -p /app/logs && chown -R $APP_UID /app/logs
RUN mkdir -p /app/dataprotection-keys && chown $APP_UID /app/dataprotection-keys
RUN mkdir -p /app/db && chown $APP_UID /app/db

USER $APP_UID
ENTRYPOINT ["dotnet", "Innkeep2.Server.dll"]