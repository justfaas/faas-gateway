### Builder
##FROM mcr.microsoft.com/dotnet/sdk:7.0 as build
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/nightly/sdk:8.0-preview AS build
ARG TARGETARCH
WORKDIR /app

RUN apt update && apt install libxml2-utils -y

# restore dependencies; use nuget config for private dependencies
COPY ./src/faas-gateway.csproj ./
COPY add-nuget-config.sh /tmp/
RUN chmod u+x /tmp/add-nuget-config.sh
RUN --mount=type=secret,id=nuget.config /tmp/add-nuget-config.sh nuget.config \
    dotnet restore -a $TARGETARCH

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
