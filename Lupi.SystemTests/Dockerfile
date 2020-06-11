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

COPY --from=build /source/out .
COPY --from=build /source/out ./test
COPY Lupi.SystemTests/Configurations ./configurations

# e.g. docker run --network=host lupi ./configurations/ComplexPhases.yml 
ENTRYPOINT ["./Lupi"]