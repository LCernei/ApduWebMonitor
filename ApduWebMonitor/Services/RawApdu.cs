using System.Text;
using PCSC.Iso7816;

namespace ApduWebMonitor.Services;

public class RawApdu
{
    public required Apdu Apdu { get; init; }
    
    public required  byte[] Source { get; init; }

    public static RawApdu CreateCommand(byte[] bytes) => new()
    {
        Apdu = ApduFactory.CommandFrom(bytes),
        Source = bytes,
    };
    
    public static RawApdu CreateResponse(IsoCase isoCase, byte[] bytes) => new()
    {
        Apdu = ApduFactory.ResponseFrom(isoCase, bytes),
        Source = bytes,
    };
    
    public string ToHex() => BitConverter.ToString(Source).Replace('-', ' ');
    
    public string ToReadable() => Apdu switch
    {
        CommandApdu command => ToReadable(command),
        ResponseApdu response => ToReadable(response),
        _ => throw new NotImplementedException($"Apdu type {Apdu.GetType().Name} not supported")
    };

    private static string ToReadable(CommandApdu apdu)
    {
        var data = apdu.Data is null ? string.Empty : Encoding.UTF8.GetString(apdu.Data);
        return $"{apdu.CLA:X2} {apdu.INS:X2} {apdu.P1:X2} {apdu.P2:X2} {data}";
    }

    private static string ToReadable(ResponseApdu apdu)
    {
        var data = apdu.HasData ? Encoding.UTF8.GetString(apdu.GetData()) : string.Empty;
        return $"{apdu.SW1:X2} {apdu.SW2:X2} {data}";
    }
}
