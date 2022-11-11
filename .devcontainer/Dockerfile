ARG VARIANT=6.0-focal
FROM mcr.microsoft.com/dotnet/sdk:${VARIANT}

RUN wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
  && dpkg -i packages-microsoft-prod.deb \
  && rm packages-microsoft-prod.deb

RUN apt-get update && apt-get install -y dotnet-sdk-7.0
