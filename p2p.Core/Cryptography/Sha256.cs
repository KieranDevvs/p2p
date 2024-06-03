using System.Security.Cryptography;
using System.Text;

namespace p2p.Core.Cryptography;

public static class Sha256
{
    public static string ComputeHash(string input)
    {
        byte[] data = Encoding.UTF8.GetBytes(input);
        data = SHA256.HashData(data);

        var hash = new StringBuilder();

        foreach (byte _byte in data)
        {
            hash.Append(_byte.ToString("X2"));
        }

        return hash.ToString().ToUpper();
    }

    public static byte[] ComputeHash(byte[] input)
    {
        return SHA256.HashData(input);
    }
}
