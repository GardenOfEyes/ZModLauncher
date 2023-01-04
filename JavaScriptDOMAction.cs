using System.Threading.Tasks;

namespace ZModLauncher;

public class JavaScriptDOMAction
{
    public string Action;
    public SignInClient Client;

    public async Task<string> Execute()
    {
        return await Client.BrowserPage.browser.ExecuteScriptAsync(Action);
    }
}