FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source

COPY Lupi/ .
RUN dotnet restore
RUN dotnet publish -c Release -o out

# runtime image
FROM mcr.microsoft.com/dotnet/runtime:5.0
WORKDIR /app

COPY --from=build /source/out .
ENTRYPOINT ["./Lupi"]