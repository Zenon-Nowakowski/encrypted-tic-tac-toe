using System.Net.Sockets;
using TicTacToe.Common;

bool selfTest = false;
for (int a = 0; a < args.Length; a++)
{
    if (args[a] == "--selftest")
    {
        selfTest = true;
        break;
    }
}
if (selfTest)
{
    bool ok = true;
    for (int m = 0; m < 9; m++)
    {
        if (LightweightCrypto.RoundTripOk(m) == false)
        {
            ok = false;
            break;
        }
    }
    Console.WriteLine(ok ? "Crypto self-test: all moves 0-8 round-trip OK." : "Crypto self-test FAILED.");
    return ok ? 0 : 1;
}

string host = "127.0.0.1";
bool hostGiven = false;
for (int a = 0; a < args.Length; a++)
{
    if (args[a].StartsWith('-') == false)
    {
        host = args[a];
        hostGiven = true;
        break;
    }
}
if (hostGiven == false)
{
    host = PromptHost();
}
const char me = 'O', foe = 'X';
var board = new GameBoard();

Console.WriteLine($"Connecting to {host}:{NetProtocol.Port}...");
using var peer = new TcpClient();
try
{
    peer.Connect(host, NetProtocol.Port);
}
catch (Exception ex)
{
    Console.WriteLine($"Could not connect: {ex.Message}");
    return 1;
}
Console.WriteLine("Connected. Host (X) moves first - waiting...");
using var stream = peer.GetStream();
using var reader = new StreamReader(stream);
using var writer = new StreamWriter(stream) { AutoFlush = true };

try
{
    while (!board.IsGameOver())
    {
        Console.WriteLine("Waiting for host's encrypted move...");
        int foeMove = NetProtocol.ReceiveEncryptedMove(reader);
        board.TryPlace(foeMove, foe);
        Console.WriteLine($"Received encrypted move {foeMove + 1} (decrypted end-to-end).");
        if (board.IsGameOver()) break;

        board.Render();
        int myMove = NetProtocol.ReadLocalMove(board, me);
        board.TryPlace(myMove, me);
        NetProtocol.SendEncryptedMove(writer, myMove);
        Console.WriteLine($"Sent encrypted move {myMove + 1}.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Connection error: {ex.Message}");
    return 1;
}

board.Render();
Console.WriteLine(board.GetWinner() is { } w ? (w == me ? "You (O) win!" : "Host (X) wins.") : "Draw!");
return 0;

static string PromptHost()
{
    Console.Write("Host IP/hostname [127.0.0.1]: ");
    string? input = Console.ReadLine();
    return string.IsNullOrWhiteSpace(input) ? "127.0.0.1" : input.Trim();
}
