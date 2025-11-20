using System.Text;
using PCSC.Iso7816;

namespace ApduWebMonitor.Services;

public class ApduTransaction
{
    public CommandApdu? CommandApdu { get; private set; }
    public byte[]? CommandBytes { get; private set; }

    public ResponseApdu? ResponseApdu { get; private set; }
    public byte[]? ResponseBytes { get; private set; }

    public void SetCommand(byte[] bytes)
    {
        if (CommandBytes is not null)
            throw new InvalidOperationException("Command is already set");

        CommandBytes = bytes;
        CommandApdu = ApduFactory.CommandFrom(bytes);
    }

    public void SetResponse(byte[] bytes)
    {
        if (CommandApdu is null)
            throw new InvalidOperationException("Command is not set");

        ResponseBytes = bytes;
        ResponseApdu = ApduFactory.ResponseFrom(CommandApdu.Case, bytes);
    }

    public string ToCommandString(bool isReadable) => isReadable ? ToReadable(CommandApdu) : ToHex(CommandBytes);

    public string ToResponseString(bool isReadable) => isReadable ? ToReadable(ResponseApdu) : ToHex(ResponseBytes);

    private static string ToHex(byte[]? bytes) => bytes is not null ? BitConverter.ToString(bytes).Replace('-', ' ') : string.Empty;

    private static string ToReadable(CommandApdu? apdu)
    {
        if (apdu is null)
            return "";

        var data = apdu.Data is null ? string.Empty : ToReadable(apdu.Data);
        return $"{apdu.CLA:X2} {apdu.INS:X2} {apdu.P1:X2} {apdu.P2:X2} {data}".Trim();
    }

    private static string ToReadable(ResponseApdu? apdu)
    {
        if (apdu is null)
            return "";

        var data = apdu.HasData ? ToReadable(apdu.GetData()) : string.Empty;
        return $"{data} {apdu.SW1:X2} {apdu.SW2:X2}".Trim();
    }

    private static string ToReadable(byte[] bytes) => string.Join(string.Empty, Encoding.UTF8.GetString(bytes).Select(x => char.IsControl(x) ? '\u0ffd' : x));

    public string ToString(bool isReadable) => ToCommandString(isReadable) + Environment.NewLine + ToResponseString(isReadable);
}
