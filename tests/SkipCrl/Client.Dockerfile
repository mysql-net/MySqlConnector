FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /src
COPY . .
RUN apt-get update \
 && apt-get install -y --no-install-recommends ca-certificates \
 && cp /src/tests/SkipCrl/generated/ca-cert.pem /usr/local/share/ca-certificates/mysqlconnector-skipcrl-ca.crt \
 && update-ca-certificates \
 && rm -rf /var/lib/apt/lists/*
ENV CRL_CA_CERT=/src/tests/SkipCrl/generated/ca-cert.pem
ENV CRL_SERVER_CERT=/src/tests/SkipCrl/generated/server-cert.pem
ENV CRL_USE_TRUST_STORE=true
CMD ["dotnet", "./tests/SkipCrl/SkipCrl.cs"]
