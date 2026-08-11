ARG VARIANT=10.0-noble
FROM mcr.microsoft.com/devcontainers/dotnet:${VARIANT}

RUN apt-get update \
  && export DEBIAN_FRONTEND=noninteractive \
  && apt-get install -y --no-install-recommends default-mysql-client \
  && rm -rf /var/lib/apt/lists/*
