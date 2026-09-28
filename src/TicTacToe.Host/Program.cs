using System.Net;
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
    Console.WriteLine($"Sample: move 4 -> {LightweightCrypto.EncryptMove(4)}");
    return ok ? 0 : 1;
}

const char me = 'X', foe = 'O';
var board = new GameBoard();
var listener = new TcpListener(IPAddress.Any, NetProtocol.Port);
listener.Start();
Console.WriteLine($"Hosting encrypted tic-tac-toe on port {NetProtocol.Port}...");
Console.WriteLine("Waiting for client (Player O) to connect...");

using TcpClient peer = listener.AcceptTcpClient();
Console.WriteLine($"Client connected from {peer.Client.RemoteEndPoint}. You (X) move first.");
using var stream = peer.GetStream();
using var reader = new StreamReader(stream);
using var writer = new StreamWriter(stream) { AutoFlush = true };

try
{
    while (!board.IsGameOver())
    {
        board.Render();
        int myMove = NetProtocol.ReadLocalMove(board, me);
        board.TryPlace(myMove, me);
        NetProtocol.SendEncryptedMove(writer, myMove);
        Console.WriteLine($"Sent encrypted move {myMove + 1}.");
        if (board.IsGameOver()) break;

        Console.WriteLine("Waiting for opponent's encrypted move...");
        int foeMove = NetProtocol.ReceiveEncryptedMove(reader);
        board.TryPlace(foeMove, foe);
        Console.WriteLine($"Received encrypted move {foeMove + 1} (decrypted end-to-end).");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Connection error: {ex.Message}");
    return 1;
}

board.Render();
Console.WriteLine(board.GetWinnerMessage(me, foe));
Console.WriteLine("Press Enter to exit...");
Console.ReadKey();
return 0;
