FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

COPY TicketBookingPlatform.slnx ./
COPY Directory.Build.props ./
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Application/Application.csproj src/Application/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/WebApi/WebApi.csproj src/WebApi/
RUN dotnet restore src/WebApi/WebApi.csproj

COPY src/ src/

ARG APP_VERSION="0.0.0"
ARG APP_COMMIT=""
RUN dotnet publish src/WebApi/WebApi.csproj -c Release -o /app/publish --no-restore \
    -p:Version="${APP_VERSION#v}" -p:SourceRevisionId="${APP_COMMIT}"

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install --no-install-recommends -y curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "WebApi.dll"]
