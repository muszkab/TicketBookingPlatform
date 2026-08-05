using System;
using System.Security.Cryptography;
using System.Text;

namespace Domain.Tickets;

/// <summary>
/// Generates a short, human-friendly, unambiguous ticket code using Crockford's Base32 alphabet
/// </summary>
public static class TicketCode
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    private const int Length = 10;

    public static string New()
    {
        Span<byte> bytes = stackalloc byte[Length];
        RandomNumberGenerator.Fill(bytes);

        var sb = new StringBuilder(Length);
        for (int i = 0; i < Length; i++)
        {
            sb.Append(Alphabet[bytes[i] & 0x1F]);
        }

        return sb.ToString();
    }
}
