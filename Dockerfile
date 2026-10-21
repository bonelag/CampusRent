FROM node:22-bookworm-slim AS web
WORKDIR /src/client
COPY client/package*.json ./
RUN npm ci
COPY client/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS api
WORKDIR /src
COPY server/CampusRent.Server.csproj server/
RUN dotnet restore server/CampusRent.Server.csproj
COPY server/ server/
RUN dotnet publish server/CampusRent.Server.csproj -c Release --no-restore -o /publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=api /publish ./
COPY --from=web /src/client/dist ./wwwroot
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "CampusRent.Server.dll"]

