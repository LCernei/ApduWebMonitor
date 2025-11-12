using System.Collections.ObjectModel;
using PCSC.Iso7816;

namespace ApduWebMonitor.Services;

public class ApduStore(ILogger<ApduStore> logger)
{
    private readonly ObservableCollection<Apdu> messages = [];
    private IsoCase lastIsoCase;
    private bool nextIsCommand = true;

    public ReadOnlyObservableCollection<Apdu> GetObservable() => new(messages);

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
