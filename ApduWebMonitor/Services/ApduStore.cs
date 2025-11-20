using System.Collections.ObjectModel;
using PCSC.Iso7816;

namespace ApduWebMonitor.Services;

public class ApduStore(ILogger<ApduStore> logger)
{
    private readonly ObservableCollection<ApduTransaction> messages = [];
    private ApduTransaction? currentTransaction;

    public ReadOnlyObservableCollection<ApduTransaction> GetObservable() => new(messages);

    public void AddMessage(byte[] apduBytes)
    {
        try
        {
            if (currentTransaction is null)
            {
                currentTransaction = new ApduTransaction();
                currentTransaction.SetCommand(apduBytes);
            }
            else
            {
                currentTransaction.SetResponse(apduBytes);
                messages.Add(currentTransaction);
                currentTransaction = null;
            }
        }
        catch (Exception ex)
        {
            currentTransaction = null;
            logger.LogError(ex, "ERROR");
        }
    }

    public void Clear()
    {
        currentTransaction = null;
        messages.Clear();
    }
}
