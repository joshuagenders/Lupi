FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source

COPY Lupi/ .
RUN dotnet restore
RUN dotnet publish -c Release -o out

# runtime image
FROM browserless/chrome:1.44-chrome-stable
WORKDIR /app

USER root
RUN wget https://packages.microsoft.com/config/debian/10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && apt-get -y update \
    && apt-get install -y apt-transport-https \
    && apt-get -y update \
    && apt-get install -y dotnet-runtime-5.0 \
    && apt-get clean

COPY --from=build /source/out .
ENTRYPOINT ["./Lupi"]