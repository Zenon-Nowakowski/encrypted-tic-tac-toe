# Encrypted Tic-Tac-Toe (.NET 10)
# One image carries both binaries; pick the role at runtime via $ROLE.
#   host (default):  docker run --rm -it -p 10000:10000 encrypted-tic-tac-toe
#   client:          docker run --rm -it -e ROLE=client encrypted-tic-tac-toe <host-address>
#   selftest:        docker run --rm encrypted-tic-tac-toe --selftest

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY EncryptedTicTacToe.slnx ./
COPY src/TicTacToe.Common/TicTacToe.Common.csproj src/TicTacToe.Common/
COPY src/TicTacToe.Host/TicTacToe.Host.csproj src/TicTacToe.Host/
COPY src/TicTacToe.Client/TicTacToe.Client.csproj src/TicTacToe.Client/
RUN dotnet restore EncryptedTicTacToe.slnx
COPY . .
# Drop any host-built bin/obj that slipped past .dockerignore, so the Linux
# SDK never reads stale Windows NuGet asset files (e.g. VS fallback folders).
RUN rm -rf src/TicTacToe.Common/bin src/TicTacToe.Common/obj \
           src/TicTacToe.Host/bin src/TicTacToe.Host/obj \
           src/TicTacToe.Client/bin src/TicTacToe.Client/obj
RUN dotnet publish src/TicTacToe.Host/TicTacToe.Host.csproj -c Release -o /app/host \
 && dotnet publish src/TicTacToe.Client/TicTacToe.Client.csproj -c Release -o /app/client

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/host ./host
COPY --from=build /app/client ./client
EXPOSE 10000
ENTRYPOINT ["sh", "-c", "if [ \"$ROLE\" = client ]; then exec dotnet /app/client/TicTacToe.Client.dll \"$@\"; else exec dotnet /app/host/TicTacToe.Host.dll \"$@\"; fi", "--"]
