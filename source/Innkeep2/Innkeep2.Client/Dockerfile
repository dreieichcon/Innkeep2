FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# The whole source tree is needed before restore, as the client references several sibling projects.
COPY . .
RUN dotnet restore "Innkeep2.Client/Innkeep2.Client.csproj"
WORKDIR "/src/Innkeep2.Client"
RUN dotnet publish "./Innkeep2.Client.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
# Prices are formatted with the "C" format, which needs the German culture for the euro format.
ENV LC_ALL=de-DE.UTF-8
ENV LANG=de-DE.UTF-8
ENV TZ=Europe/Berlin
WORKDIR /app
COPY --from=publish /app/publish .

USER root
RUN mkdir -p /app/log && chown $APP_UID /app/log
RUN mkdir -p /app/db && chown $APP_UID /app/db
RUN mkdir -p /app/dataprotection-keys && chown $APP_UID /app/dataprotection-keys

USER $APP_UID
ENTRYPOINT ["dotnet", "Innkeep2.Client.dll"]
