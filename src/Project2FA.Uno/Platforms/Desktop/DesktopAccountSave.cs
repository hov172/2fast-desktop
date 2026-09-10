#if TWOFAST_DESKTOP
using Project2FA.Repository.Models;
namespace Project2FA.Services;
public partial class DataService
{
    internal void ClearLockedAccounts()
    {
        Collection.CollectionChanged -= Accounts_CollectionChanged;
        try
        {
            Collection.Clear();
            GlobalCategories.Clear();
            ACVCollection.Filter = null;
            EmptyAccountCollectionTipIsOpen = false;
            TOTPEventStopwatch.Stop();
        }
        finally { Collection.CollectionChanged += Accounts_CollectionChanged; }
    }
    private readonly AccountCommitQueue<TwoFACodeModel> accountCommits = new();
    public async Task<bool> AddDesktopAccount(TwoFACodeModel model)
    {
        // Await durable save before navigating: Accounts initialization reloads
        // the vault and must not race the collection's async event handler.
        bool saved = await accountCommits.Commit(model, Collection.Contains,
            item => ChangeAccountWithoutAutosave(item, true),
            item => ChangeAccountWithoutAutosave(item, false), WriteLocalDatafile);
        if (saved) await ResetCollection();
        return saved;
    }
    private void ChangeAccountWithoutAutosave(TwoFACodeModel item, bool add)
    {
        Collection.CollectionChanged -= Accounts_CollectionChanged;
        try { if (add) Collection.Add(item); else Collection.Remove(item); }
        finally { Collection.CollectionChanged += Accounts_CollectionChanged; }
    }
}
#endif
