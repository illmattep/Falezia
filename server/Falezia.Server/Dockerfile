# FALEZIA · server dedicato
# Build dal root del repo:  docker build -f server/Falezia.Server/Dockerfile -t falezia/server .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# prima i csproj: il layer delle dipendenze resta in cache
COPY shared/Falezia.Core/Falezia.Core.csproj shared/Falezia.Core/
COPY server/Falezia.Server/Falezia.Server.csproj server/Falezia.Server/
RUN dotnet restore server/Falezia.Server/Falezia.Server.csproj

# poi i sorgenti condivisi e il server
COPY game/sim/ game/sim/
COPY game/net/ game/net/
COPY game/maps/calanera.json game/maps/calanera.json
COPY server/Falezia.Server/ server/Falezia.Server/

RUN dotnet publish server/Falezia.Server/Falezia.Server.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV FALEZIA_PORT=27015 \
    FALEZIA_MAX=10 \
    DOTNET_EnableDiagnostics=0

EXPOSE 27015/udp
USER 1000:1000
ENTRYPOINT ["dotnet", "Falezia.Server.dll"]
