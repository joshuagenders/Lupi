FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /usr/local/src

COPY Lupi/ .
RUN dotnet restore
RUN dotnet publish -c Release -o out

# runtime image
FROM mcr.microsoft.com/dotnet/runtime:5.0

COPY --from=build /usr/local/src/out /usr/local/bin/

ENTRYPOINT ["./Lupi"]