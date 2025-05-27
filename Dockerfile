# syntax=docker/dockerfile:1.3

# Create a stage for building the application.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

COPY src /source

WORKDIR /source

# This is the architecture you’re building for, which is passed in by the builder.
# Placing it here allows the previous steps to be cached across architectures.
ARG TARGETARCH
ENV HOME=/tmp/${TARGETARCH}

RUN apk update && apk add --no-cache libxml2-utils

# Add the GitHub NuGet registry to the list of package sources.
RUN --mount=type=secret,id=GITHUB_TOKEN \
    dotnet nuget add source \
        --store-password-in-clear-text \
        -n justfaas \
        -u justfaas \
        -p $(cat /run/secrets/GITHUB_TOKEN) \
        https://nuget.pkg.github.com/justfaas/index.json

# Build the application.
# Leverage a cache mount to /root/.nuget/packages so that subsequent builds don't have to re-download packages.
# If TARGETARCH is "amd64", replace it with "x64" - "x64" is .NET's canonical name for this and "amd64" doesn't
#   work in .NET 6.0.
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish -a ${TARGETARCH/amd64/x64} --use-current-runtime --self-contained false -o /app

# Create a new stage for running the application that contains the minimal
# runtime dependencies for the application. This often uses a different base
# image from the build stage where the necessary files are copied from the build
# stage.
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app

# Copy everything needed to run the app from the "build" stage.
COPY --from=build /app .

# Switch to a non-privileged user (defined in the base image) that the app will run under.
# See https://docs.docker.com/go/dockerfile-user-best-practices/
# and https://github.com/dotnet/dotnet-docker/discussions/4764
USER $APP_UID

ENTRYPOINT ["dotnet", "faas-gateway.dll"]
