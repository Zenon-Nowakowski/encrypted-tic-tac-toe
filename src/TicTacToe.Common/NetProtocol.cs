// Tiny line-based network protocol. Each move travels as one text line:
//   "<16 comma-separated cipher ints>\n"
// e.g. EncryptMove(4) -> "123,456,...,789\n" (values depend on the key).
//
// Plain C# with simple loops - no LINQ, no lambdas, no shorthand.
namespace TicTacToe.Common;

public static class NetProtocol
{
    public const int Port = 10000;

    public static void SendEncryptedMove(StreamWriter writer, int move)
    {
        writer.WriteLine(LightweightCrypto.EncryptMove(move));
        writer.Flush();
    }

    public static int ReceiveEncryptedMove(StreamReader reader)
    {
        string? line = reader.ReadLine()
            ?? throw new IOException("Peer disconnected.");
        return LightweightCrypto.DecryptMove(line);
    }

    // A valid payload is 16 comma-separated ints, each in range 0 to N-1.
    public static bool IsValidPayload(string? line)
    {
        if (line is null)
        {
            return false;
        }
        string[] parts = line.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 16)
        {
            return false;
        }
        for (int j = 0; j < parts.Length; j++)
        {
            bool parsed = int.TryParse(parts[j], out int value);
            if (parsed == false)
            {
                return false;
            }
            if (value < 0 || value >= LightweightCrypto.N)
            {
                return false;
            }
        }
        return true;
    }

    public static int ReadLocalMove(GameBoard board, char mark)
    {
        while (true)
        {
            int[] free = board.FreeCells;
            string choices = "";
            for (int j = 0; j < free.Length; j++)
            {
                if (j > 0)
                {
                    choices = choices + ",";
                }
                choices = choices + (free[j] + 1).ToString();
            }
            Console.Write($"You are '{mark}'. Enter cell 1-9 [{choices}]: ");
            string? input = Console.ReadLine();
            bool parsed = int.TryParse(input, out int cell);
            bool inRange = parsed && cell >= 1 && cell <= 9;
            bool isFree = false;
            if (inRange)
            {
                for (int j = 0; j < free.Length; j++)
                {
                    if (free[j] == cell - 1)
                    {
                        isFree = true;
                        break;
                    }
                }
            }
            if (inRange && isFree)
            {
                return cell - 1;
            }
            Console.WriteLine("Invalid cell, try again.");
        }
    }
}
