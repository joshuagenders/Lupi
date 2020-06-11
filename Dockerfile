FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /source

COPY Lupi/ .
RUN dotnet restore
RUN dotnet publish -c Release -o out

# runtime image
FROM browserless/chrome:1.34-chrome-stable
WORKDIR /app

USER root
RUN wget https://packages.microsoft.com/config/debian/10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get -y update \
    && apt-get install -y dotnet-runtime-3.1 \
    && apt-get clean

COPY --from=build /source/out .
ENTRYPOINT ["./Lupi"]