FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-test
WORKDIR /source

COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM joshuagenders/lupi:slim-latest
WORKDIR /app

COPY --from=build-test /source/out ./test
COPY ./Configurations ./configurations

ENV RESULT_PATH results.log
ENV STATSD_HOST 127.0.0.1
ENV STATSD_PORT 8125
ENV STATSD_PREFIX Lupi.Examples

# e.g. docker run --network=host lupi ./configurations/ConcurrencyOnly.yml ]
ENTRYPOINT ["./Lupi"]