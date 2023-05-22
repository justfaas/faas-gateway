### Builder
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:7.0 as build
ARG TARGETARCH
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
WORKDIR /app

RUN apt update && apt install libxml2-utils -y

# restore dependencies; use nuget config for private dependencies
COPY ./src/faas-gateway.csproj ./
RUN --mount=type=secret,id=GITHUB_TOKEN \
    dotnet nuget add source \
        --store-password-in-clear-text \
        -n justfaas \
        -u justfaas \
        -p $(cat /run/secrets/GITHUB_TOKEN) \
        https://nuget.pkg.github.com/justfaas/index.json
RUN dotnet restore -a $TARGETARCH

COPY ./src/. ./
RUN dotnet publish -c release -a $TARGETARCH -o dist faas-gateway.csproj

### Runtime
FROM mcr.microsoft.com/dotnet/aspnet:7.0 as final

RUN addgroup faas-app && useradd -G faas-app faas-user

WORKDIR /app
COPY --from=build /app/dist/ ./
RUN chown faas-user:faas-app -R .

USER faas-user

ENTRYPOINT [ "dotnet", "faas-gateway.dll" ]
