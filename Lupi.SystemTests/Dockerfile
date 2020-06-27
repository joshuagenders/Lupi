FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /source

COPY Lupi/ .
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-test
WORKDIR /source

COPY Lupi.Examples/ .
RUN dotnet restore
RUN dotnet publish -c Release -o out

# runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:3.1
WORKDIR /app

RUN apt-get update && \
    apt-get install -yq gconf-service libasound2 libatk1.0-0 libc6 libcairo2 libcups2 libdbus-1-3 \
    libexpat1 libfontconfig1 libgcc1 libgconf-2-4 libgdk-pixbuf2.0-0 libglib2.0-0 libgtk-3-0 libnspr4 \
    libpango-1.0-0 libpangocairo-1.0-0 libstdc++6 libx11-6 libx11-xcb1 libxcb1 libxcomposite1 \
    libxcursor1 libxdamage1 libxext6 libxfixes3 libxi6 libxrandr2 libxrender1 libxss1 libxtst6 \
    ca-certificates fonts-liberation libappindicator1 libnss3 lsb-release xdg-utils wget

COPY --from=build /source/out .
COPY --from=build-test /source/out ./test
COPY Lupi.SystemTests/Configurations ./configurations

ENV RESULT_PATH results.log
ENV STATSD_HOST 127.0.0.1
ENV STATSD_PORT 8125
ENV STATSD_PREFIX Lupi.Examples

# e.g. docker run --network=host lupi ./configurations/ComplexPhases.yml 
ENTRYPOINT ["./Lupi"]