// End-to-end encryption for tic-tac-toe moves.
// Custom lightweight algorithm ported from:
//   https://github.com/Zenon-Nowakowski/IoT-lightweight-encryption-method (main.py)
//
// Stage 1 (PRESENT-style SP-network, 64-bit block):
//   C1 = AddRoundKey(plaintext, k)   // XOR with pre-shared key
//   C2 = SBoxLayer(C1)               // 4-bit SBOX on each nibble
//   C3 = PLayer(C2)                  // bit permutation via pTable
//
// Stage 2 (per-nibble public-key layer from the same repo):
//   each hex nibble m (0-15) -> c = m^e mod n  (p=47, q=71, n=3337, e=97, d=1693)
//
// A move (0-8) is encoded as a 64-bit block "0x000000000000000<M>"
// so the wire payload reveals nothing about the move without the key.
// Both peers pre-share k/e/d/n, giving end-to-end confidentiality of moves.
//
// Plain C# with simple loops - no LINQ, no lambdas, no shorthand.
using System.Numerics;
using System.Text;

namespace TicTacToe.Common;

public static class LightweightCrypto
{
    // ---- Stage 1 parameters (from main.py) ----
    public const string RoundKeyHex = "0x0123456789ABCDEF";

    private static readonly int[] SBox =
    [
        0xC, 0x5, 0x6, 0xB, 0x9, 0x0, 0xA, 0xD,
        0x3, 0xE, 0xF, 0x8, 0x4, 0x7, 0x1, 0x2,
    ];

    private static readonly int[] SBoxInverse =
    [
        0x5, 0xE, 0xF, 0x8, 0xC, 0x1, 0x2, 0xD,
        0xB, 0x4, 0x6, 0x3, 0x0, 0x7, 0x9, 0xA,
    ];

    private static readonly int[] PTable =
    [
        0, 16, 32, 48, 1, 17, 33, 49, 2, 18, 34, 50, 3, 19, 35, 51,
        4, 20, 36, 52, 5, 21, 37, 53, 6, 22, 38, 54, 7, 23, 39, 55,
        8, 24, 40, 56, 9, 25, 41, 57, 10, 26, 42, 58, 11, 27, 43, 59,
        12, 28, 44, 60, 13, 29, 45, 61, 14, 30, 46, 62, 15, 31, 47, 63,
    ];

    // Inverse permutation. The original Python demo re-applied PTable to
    // "decrypt", which does NOT invert the permutation; a true inverse
    // is required for a correct round-trip.
    private static readonly int[] PTableInverse = BuildInverseTable();

    private static int[] BuildInverseTable()
    {
        int[] inverse = new int[64];
        for (int i = 0; i < 64; i++)
        {
            inverse[PTable[i]] = i;
        }
        return inverse;
    }

    // ---- Stage 2 parameters (Part 2 of main.py) ----
    public const int P = 47;
    public const int Q = 71;
    public const int N = P * Q;          // 3337
    public const int PhiN = (P - 1) * (Q - 1);
    public const int E = 97;
    public const int D = 1693;

    // ============ High-level move API (what the game calls) ============

    // Encrypt a board index 0-8 into a wire-safe CSV payload of 16 ints.
    public static string EncryptMove(int move, string? keyHex = null)
    {
        if (move is < 0 or > 8)
            throw new ArgumentOutOfRangeException(nameof(move), "Move must be 0-8.");
        string block = $"0x{move:X16}"; // "0x" + 16 hex chars
        string c3 = PLayer(SBoxLayer(AddRoundKey(block, keyHex ?? RoundKeyHex)));
        int[] nibbles = PlaintextNibbles(c3);
        int[] cipher = new int[nibbles.Length];
        for (int j = 0; j < nibbles.Length; j++)
        {
            cipher[j] = ModPow(nibbles[j], E, N);
        }
        return string.Join(",", cipher);
    }

    // Decrypt a wire payload back into a board index 0-8.
    public static int DecryptMove(string payload, string? keyHex = null)
    {
        string[] parts = payload.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int[] cipher = new int[parts.Length];
        for (int j = 0; j < parts.Length; j++)
        {
            cipher[j] = int.Parse(parts[j]);
        }
        if (cipher.Length != 16)
            throw new FormatException($"Payload must hold 16 cipher ints, got {cipher.Length}.");
        int[] plain = new int[cipher.Length];
        for (int j = 0; j < cipher.Length; j++)
        {
            plain[j] = ModPow(cipher[j], D, N);
        }
        string c3 = NibblesToHex(plain);
        string d3 = AddRoundKey(SBoxDecrypt(InversePLayer(c3)), keyHex ?? RoundKeyHex);
        int move = Convert.ToInt32(d3[^1..], 16);
        if (move is < 0 or > 8)
            throw new FormatException($"Decrypted move out of range: {move}.");
        return move;
    }

    // Quick self-check used by --selftest / tests.
    public static bool RoundTripOk(int move)
    {
        return DecryptMove(EncryptMove(move)) == move;
    }

    // ============ Stage 1 primitives (direct port of main.py) ============

    public static string AddRoundKey(string hexBlock, string keyHex)
    {
        return ToHex(FromHex(hexBlock) ^ FromHex(keyHex));
    }

    public static string SBoxLayer(string hexBlock)
    {
        int[] nibbles = PlaintextNibbles(hexBlock);
        int[] mapped = new int[nibbles.Length];
        for (int j = 0; j < nibbles.Length; j++)
        {
            mapped[j] = SBox[nibbles[j]];
        }
        return NibblesToHex(mapped);
    }

    public static string SBoxDecrypt(string hexBlock)
    {
        int[] nibbles = PlaintextNibbles(hexBlock);
        int[] mapped = new int[nibbles.Length];
        for (int j = 0; j < nibbles.Length; j++)
        {
            mapped[j] = SBoxInverse[nibbles[j]];
        }
        return NibblesToHex(mapped);
    }

    public static string PLayer(string hexBlock)
    {
        return Permute(hexBlock, PTable);
    }

    public static string InversePLayer(string hexBlock)
    {
        return Permute(hexBlock, PTableInverse);
    }

    private static string Permute(string hexBlock, int[] table)
    {
        string bits = ToBin64(FromHex(hexBlock));
        StringBuilder permuted = new StringBuilder(64);
        for (int j = 0; j < table.Length; j++)
        {
            permuted.Append(bits[table[j]]);
        }
        return ToHex(Convert.ToUInt64(permuted.ToString(), 2));
    }

    // ============ Stage 2 primitives (direct port of main.py) ============

    public static int EncryptNibble(int m)
    {
        return ModPow(m, E, N);
    }

    public static int DecryptNibble(int c)
    {
        return ModPow(c, D, N);
    }

    private static int ModPow(int value, int exp, int mod)
    {
        return (int)BigInteger.ModPow(new BigInteger(value), new BigInteger(exp), new BigInteger(mod));
    }

    // ============ Hex / nibble helpers ============

    private static ulong FromHex(string hex)
    {
        string digits = hex;
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            digits = hex[2..];
        }
        return Convert.ToUInt64(digits, 16);
    }

    private static string ToHex(ulong value)
    {
        return "0x" + value.ToString("X16");
    }

    private static string ToBin64(ulong value)
    {
        return Convert.ToString((long)value, 2).PadLeft(64, '0');
    }

    // "0x28B4..." -> [2, 8, 11, ...] (16 nibbles, MSB first)
    public static int[] PlaintextNibbles(string hexBlock)
    {
        string digits = hexBlock;
        if (hexBlock.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            digits = hexBlock[2..];
        }
        digits = digits.PadLeft(16, '0');
        int[] nibbles = new int[digits.Length];
        for (int j = 0; j < digits.Length; j++)
        {
            nibbles[j] = Convert.ToInt32(digits[j].ToString(), 16);
        }
        return nibbles;
    }

    // [2, 8, 11, ...] -> "0x28B4..."
    public static string NibblesToHex(int[] nibbles)
    {
        string hex = "0x";
        for (int j = 0; j < nibbles.Length; j++)
        {
            hex = hex + nibbles[j].ToString("X");
        }
        return hex;
    }
}
