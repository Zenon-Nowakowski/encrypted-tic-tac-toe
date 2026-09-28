# Encrypted Tic-Tac-Toe (.NET)

Very simple 2-player tic-tac-toe: **Host (X)** listens on TCP `10000`, **Client (O)** connects. Every move is **end-to-end encrypted** with a custom lightweight cipher before it hits the network.

## Layout

- `src/TicTacToe.Common/LightweightCrypto.cs` — **separate E2E encryption script** (custom algorithm, ported from [`IoT-lightweight-encryption-method`](https://github.com/Zenon-Nowakowski/IoT-lightweight-encryption-method)):
  - Stage 1 (PRESENT-style, 64-bit block): `AddRoundKey XOR k` → 4-bit `SBOX` → bit-permutation `pTable` (`k = 0x0123456789ABCDEF`).
  - Stage 2 (per-nibble): `c = m^e mod n` with `p=47, q=71, n=3337, e=97, d=1693`.
  - A move `0-8` is encoded as block `0x000000000000000<M>`, so the wire payload (16 comma-separated ints, one line per move) reveals nothing without the pre-shared key.
  - Fix vs the Python demo: decryption uses a true **inverse** permutation (`PTableInverse`); re-applying `pTable` does not invert the permutation.
- `src/TicTacToe.Common/GameBoard.cs` — rules + win/draw check with plain loops.
- `src/TicTacToe.Common/NetProtocol.cs` — line-framed TCP send/receive + validation, payload parsing with plain loops.
- `src/TicTacToe.Host/` — Player X, listens, moves first.
- `src/TicTacToe.Client/` — Player O, connects, moves second.

## Run

```powershell
dotnet build
# terminal 1 (host)
dotnet run --project src/TicTacToe.Host
# terminal 2 (client; optional host arg, default 127.0.0.1)
dotnet run --project src/TicTacToe.Client -- 127.0.0.1
```

Enter cells `1-9` (empty cells are shown as their number).

## Crypto self-test (no network)

```powershell
dotnet run --project src/TicTacToe.Host -- --selftest
```

Checks all moves `0-8` round-trip through `DecryptMove(EncryptMove(m))`.

## Docker

One image carries both binaries; the role is picked at runtime via `$ROLE`.

```bash
./build.sh                        # builds encrypted-tic-tac-toe
./run.sh selftest                 # crypto round-trip check, no network
./run.sh host                     # Player X, publishes TCP 10000
./run.sh client [server]          # Player O, default server: host.docker.internal
```

`IMAGE=my-name ./build.sh` / `IMAGE=my-name ./run.sh ...` overrides the image name.
On Linux, point the client at the host's LAN IP instead of `host.docker.internal`.
