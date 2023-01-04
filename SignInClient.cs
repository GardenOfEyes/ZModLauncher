using System;
using System.Threading.Tasks;
using ZModLauncher.Pages;
using static ZModLauncher.UIHelper;
using static ZModLauncher.GlobalStringConstants;

namespace ZModLauncher;

public abstract class SignInClient : OAuthConstants
{
    public readonly SignInBrowser BrowserPage;

    protected SignInClient(SignInBrowser browserPage)
    {
        BrowserPage = browserPage;
        BrowserPage.browser.NavigationCompleted += (_, _) => { CheckUserMembership(); };
    }

    public abstract void CheckUserMembership();

    public string GetBrowserUrl()
    {
        return BrowserPage.browser.Source.ToString();
    }

    public void SetBrowserUrl(string url)
    {
        BrowserPage.browser.Source = new Uri(url);
    }

    public async Task<string> ExecuteDOMAction(string domAction)
    {
        return await new JavaScriptDOMAction
        {
            Action = domAction,
            Client = this
        }.Execute();
    }

    private bool BrowserUrlContainsString(string str)
    {
        return GetBrowserUrl().IndexOf(str, StringComparison.Ordinal) != -1;
    }

    public bool IsOnRedirectPage()
    {
        return GetBrowserUrl().StartsWith(RedirectUri);
    }

    public bool IsOnLoginPage()
    {
        return BrowserUrlContainsString("login");
    }

    public bool IsOnAuthorizePage()
    {
        return BrowserUrlContainsString("authorize");
    }

    public async Task<bool> IsConnectedToInternet()
    {
        string result = await ExecuteDOMAction("Array.from(document.querySelectorAll('div'))\r\n.find(el => el.textContent.includes(\"You're not connected\"))");
        return result.IndexOf(NoInternetResponse, StringComparison.Ordinal) == -1;
    }

    public void SendBackToSignInPage()
    {
        NavigateToPage(SignInPageName);
    }
}