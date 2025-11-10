using PCSC;
using PCSC.Iso7816;

namespace ApduWebMonitor.Services;

public class ApduFactory
{
    public static CommandApdu CommandFrom(params byte[] apdu)
    {
        if (apdu.Length < 4)
            throw new ArgumentException("Command APDU must have at least 4 bytes (CLA, INS, P1, P2).");

        var span = apdu[4..];
        if (span.Length == 0)
        {
            // Case 1
            return CreateCase1Command(apdu);
        }

        if (span[0] != 0x00)
        {
            // --- Short length path ---
            // Either Case 2s (only Le), or Case 3s/4s (Lc + Data [+ Le])
            if (span.Length == 1)
            {
                // Case 2s: single-byte Le
                return CreateCase2sCommand(apdu, span[0]);
            }

            // Lc is present (1 byte)
            var lc = span[0];
            if (lc == 0)
                throw new FormatException("Short Lc must be 1..255 when present.");

            if (span.Length < 1 + lc)
                throw new FormatException("APDU truncated: not enough bytes for Data.");

            var data = span[1..lc];
            var rest = span[(1 + lc)..];

            if (rest.Length == 0)
            {
                // Case 3s
                return CreateCase3sCommand(apdu, data);
            }
            else if (rest.Length == 1)
            {
                // Case 4s: trailing single-byte Le
                return CreateCase4sCommand(apdu, data, rest[0]);
            }
            else
            {
                throw new FormatException("Invalid short APDU: bytes remain after Data that don't match a 1-byte Le.");
            }
        }
        else
        {
            var after00 = span[1..];
            if (after00.Length == 0)
            {
                // Case 2s: apduended in 00 Le
                return CreateCase2sCommand(apdu, 0);
            }

            // --- Extended length path ---
            // span[0] == 0x00 signals extended length

            if (after00.Length < 2)
                throw new FormatException("Extended APDU requires at least two bytes after the 0x00 sentinel.");

            // Two possibilities:
            // - Case 2e: 00 Le1 Le2 (and nothing else)
            // - Case 3e/4e: 00 Lc1 Lc2 Data [Le1 Le2]

            if (after00.Length == 2)
            {
                // Case 2e: only Le (2 bytes)
                return CreateCase2eCommand(apdu, after00);
            }

            // We have at least 3 bytes after the initial 0x00, so interpret Lc (2 bytes)
            var lc = CalculateSize(after00[..2]);
            if (lc == 0)
                throw new FormatException("Extended Lc must be 1..65535 when present.");

            var afterLc = after00[2..];

            if (afterLc.Length < lc)
                throw new FormatException("APDU truncated: not enough bytes for extended Data.");

            var data = afterLc[..lc];
            var rest = afterLc[lc..];

            if (rest.Length == 0)
            {
                // Case 3e
                return CreateCase3eCommand(apdu, data);
            }
            else if (rest.Length == 2)
            {
                // Case 4e: trailing two-byte Le
                return CreateCase4eCommand(apdu, data, rest);
            }
            else
            {
                throw new FormatException("Invalid extended APDU: bytes remain after Data that don't match a 2-byte Le.");
            }
        }
    }

    private static CommandApdu CreateCase1Command(byte[] apdu) => new(IsoCase.Case1, SCardProtocol.T1)
    {
        CLA = apdu[0],
        INS = apdu[1],
        P1 = apdu[2],
        P2 = apdu[3],
    };

    private static CommandApdu CreateCase2sCommand(byte[] apdu, byte le) => new(IsoCase.Case2Short, SCardProtocol.T1)
    {
        CLA = apdu[0],
        INS = apdu[1],
        P1 = apdu[2],
        P2 = apdu[3],
        Le = CalculateSize(le),
    };

    private static CommandApdu CreateCase3sCommand(byte[] apdu, byte[] data) => new(IsoCase.Case3Short, SCardProtocol.T1)
    {
        CLA = apdu[0],
        INS = apdu[1],
        P1 = apdu[2],
        P2 = apdu[3],
        Data = data,
    };

    private static CommandApdu CreateCase4sCommand(byte[] apdu, byte[] data, byte le) => new(IsoCase.Case4Short, SCardProtocol.T1)
    {
        CLA = apdu[0],
        INS = apdu[1],
        P1 = apdu[2],
        P2 = apdu[3],
        Data = data,
        Le = CalculateSize(le),
    };

    private static CommandApdu CreateCase2eCommand(byte[] apdu, byte[] le) => new(IsoCase.Case2Extended, SCardProtocol.T1)
    {
        CLA = apdu[0],
        INS = apdu[1],
        P1 = apdu[2],
        P2 = apdu[3],
        Le = CalculateSize(le),
    };

    private static CommandApdu CreateCase3eCommand(byte[] apdu, byte[] data) => new(IsoCase.Case3Extended, SCardProtocol.T1)
    {
        CLA = apdu[0],
        INS = apdu[1],
        P1 = apdu[2],
        P2 = apdu[3],
        Data = data,
    };

    private static CommandApdu CreateCase4eCommand(byte[] apdu, byte[] data, byte[] le) => new(IsoCase.Case4Extended, SCardProtocol.T1)
    {
        CLA = apdu[0],
        INS = apdu[1],
        P1 = apdu[2],
        P2 = apdu[3],
        Data = data,
        Le = CalculateSize(le),
    };

    private static int CalculateSize(params byte[] bytes)
    {
        if (bytes.Length == 1)
            return bytes[0];

        if (bytes.Length == 2)
            return (bytes[0] << 8) | bytes[1];

        throw new ArgumentException($"Length {bytes.Length} size not supported");
    }

    public static ResponseApdu ResponseFrom(IsoCase isoCase, params byte[] apdu) => new(apdu, isoCase, SCardProtocol.T1);
}
