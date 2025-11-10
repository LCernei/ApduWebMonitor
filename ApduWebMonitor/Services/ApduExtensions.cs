using System.Text;
using PCSC.Iso7816;

namespace ApduWebMonitor.Services;

public static class ApduExtensions
{
    public static string ToHex(this Apdu apdu)
        => BitConverter.ToString(apdu.ToArray()).Replace('-', ' ');

    public static string ToReadable(this Apdu apdu) => apdu switch
    {
        CommandApdu command => command.ToReadable(),
        ResponseApdu response => response.ToReadable(),
        _ => throw new NotImplementedException($"Apdu type {apdu.GetType().Name} not supported")
    };

    public static string ToReadable(this CommandApdu apdu)
    {
        var data = apdu.Data is null ? string.Empty : Encoding.UTF8.GetString(apdu.Data);
        return $"{apdu.CLA:X2} {apdu.INS:X2} {apdu.P1:X2} {apdu.P2:X2} {data}";
    }

    public static string ToReadable(this ResponseApdu apdu)
    {
        var data = apdu.HasData ? Encoding.UTF8.GetString(apdu.GetData()) : string.Empty;
        return $"{apdu.SW1:X2} {apdu.SW2:X2} {data}";
    }
}
