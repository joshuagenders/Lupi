FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /usr/local/src

COPY Lupi/ .
RUN dotnet restore
RUN dotnet publish -c Release -o out

# runtime image
FROM mcr.microsoft.com/playwright:focal
WORKDIR /usr/local/share

USER root
ADD https://packages.microsoft.com/config/ubuntu/20.10/packages-microsoft-prod.deb packages-microsoft-prod.deb
RUN dpkg -i packages-microsoft-prod.deb \
    && apt-get -y update \
    && apt-get install -y apt-transport-https \
    && apt-get -y update \
    && apt-get install -y dotnet-runtime-6.0 \
    && apt-get clean

COPY --from=build /usr/local/src/out /usr/local/bin

ENTRYPOINT ["Lupi"]