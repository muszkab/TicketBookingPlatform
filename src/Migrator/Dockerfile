FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

COPY Directory.Build.props ./
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Application/Application.csproj src/Application/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/Migrator/Migrator.csproj src/Migrator/
RUN dotnet restore src/Migrator/Migrator.csproj

COPY src/ src/

ARG APP_VERSION="0.0.0"
ARG APP_COMMIT=""
RUN dotnet publish src/Migrator/Migrator.csproj -c Release -o /app/publish --no-restore \
    -p:Version="${APP_VERSION#v}" -p:SourceRevisionId="${APP_COMMIT}"

# The migrator references Infrastructure, which pulls in the ASP.NET Core shared
# framework (JwtBearer), so the runtime image must be the aspnet one.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "Migrator.dll"]
