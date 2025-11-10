using PCSC.Iso7816;

namespace ApduWebMonitor.Services;

public class ApduStore(ILogger<ApduStore> logger)
{
    private bool nextIsCommand = true;
    private IsoCase lastIsoCase;

    private readonly List<Apdu> messages = [];

    public Action<Apdu>? MessageAdded { get; set; }
    public Action? Cleared { get; set; }

    public IReadOnlyList<Apdu> GetMessages() => messages.AsReadOnly();

    public void AddMessage(byte[] apduBytes)
    {
        try
        {
            Apdu apdu;
            if (nextIsCommand)
            {
                nextIsCommand = false;
                apdu = ApduFactory.CommandFrom(apduBytes);
                lastIsoCase = apdu.Case;
            }
            else
            {
                nextIsCommand = true;
                apdu = ApduFactory.ResponseFrom(lastIsoCase, apduBytes);
            }

            messages.Add(apdu);
            MessageAdded?.Invoke(apdu);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ERROR");
            return;
        }
    }

    public void Reset()
    {
        nextIsCommand = true;
    }

    public void Clear()
    {
        messages.Clear();
        Cleared?.Invoke();
    }
}
