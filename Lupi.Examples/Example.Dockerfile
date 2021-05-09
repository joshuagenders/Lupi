FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-test
WORKDIR /source

COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:5.0
WORKDIR /app

COPY --from=build /source/out .
COPY --from=build-test /source/out ./test
COPY ./Configurations ./configurations

ENV RESULT_PATH results.log
ENV STATSD_HOST 127.0.0.1
ENV STATSD_PORT 8125
ENV STATSD_PREFIX Lupi.Examples

# e.g. docker run --network=host lupi ./configurations/ConcurrencyOnly.yml ]
ENTRYPOINT ["./Lupi"]