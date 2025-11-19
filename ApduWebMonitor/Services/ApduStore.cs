using System.Collections.ObjectModel;
using PCSC.Iso7816;

namespace ApduWebMonitor.Services;

public class ApduStore(ILogger<ApduStore> logger)
{
    private readonly ObservableCollection<RawApdu> messages = [];
    private IsoCase lastIsoCase;
    private bool nextIsCommand = true;

    public ReadOnlyObservableCollection<RawApdu> GetObservable() => new(messages);

    public void AddMessage(byte[] apduBytes)
    {
        try
        {
            RawApdu apdu;
            if (nextIsCommand)
            {
                nextIsCommand = false;
                apdu = RawApdu.CreateCommand(apduBytes);
                lastIsoCase = apdu.Apdu.Case;
            }
            else
            {
                nextIsCommand = true;
                apdu = RawApdu.CreateResponse(lastIsoCase, apduBytes);
            }

            messages.Add(apdu);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ERROR");
        }
    }

    public void Reset()
    {
        nextIsCommand = true;
        Clear();
    }

    public void Clear()
    {
        messages.Clear();
    }
}
