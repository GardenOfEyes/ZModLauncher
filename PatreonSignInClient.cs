using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json.Linq;
using ZModLauncher.Pages;
using static ZModLauncher.StringHelper;
using static ZModLauncher.UIHelper;
using static ZModLauncher.GlobalStringConstants;

namespace ZModLauncher;

public class PatreonSignInClient : SignInClient
{
    public PatreonSignInClient(SignInBrowser browserPage) : base(browserPage) { }

    private async void ClickAllowButtonIfExists()
    {
        const string allowButtonDomAction = "document.getElementsByClassName(\"patreon-button patreon-button-action\")";
        string allowButtonDomElementName = await ExecuteDOMAction(allowButtonDomAction);
        if (allowButtonDomElementName != "{}") await ExecuteDOMAction($"{allowButtonDomAction}[0].click();");
    }

    private void GetSingleUseCode()
    {
        SingleUseCode = ExtractString(GetBrowserUrl(),
            "code=", "&state");
    }

    private async Task GetUserAccessToken()
    {
        UserAccessToken = (await new NetClient
        {
            RequestContent = $"code={SingleUseCode}&grant_type=authorization_code&client_id="
                + $"{ClientId}&client_secret={ClientSecret}&redirect_uri={RedirectUri}",
            Url = TokenUrl,
            RequestFormat = "application/x-www-form-urlencoded"
        }.POST<JObject>()).GetValue("access_token")?.ToString();
    }

    private async Task<bool> IsMembershipValid()
    {
        var configManager = new LauncherConfigManager();
        var membership = await new NetClient("Authorization", $"Bearer {UserAccessToken}")
        {
            Url =
                "https://www.patreon.com/api/oauth2/v2/identity?include=memberships.currently_entitled_tiers,memberships&fields%5Bmember%5D"
                + "=campaign_lifetime_support_cents,currently_entitled_amount_cents,email,full_name,is_follower,"
                + "last_charge_date,last_charge_status,lifetime_support_cents,next_charge_date,note,"
                + "patron_status,pledge_cadence,pledge_relationship_start,will_pay_amount_cents"
        }.GET<JObject>();
        try
        {
            var currentTierId = membership["included"]?[0]?["relationships"]?["currently_entitled_tiers"]?["data"]?[0]?["id"]?.ToString();
            if (currentTierId == configManager.LauncherConfig[RejectTierIdKey]?.ToString()) return false;
            var membershipStatus = membership["included"]?[0]?["attributes"]?["patron_status"]?.ToString();
            return membershipStatus is ActivePatronStatus or DeclinedPatronStatus;
        }
        catch
        {
            return false;
        }
    }

    private async Task AttemptToAuthorizeMembership()
    {
        if (await IsMembershipValid() && Application.Current.MainWindow != null)
            NavigateToPage(LoadingPageName);
        else
        {
            Process.Start(CreatorUrl);
            SendBackToSignInPage();
        }
    }

    public override async void CheckUserMembership()
    {
        if (!await IsConnectedToInternet())
        {
            Show(BrowserPage.browser);
            await Task.Delay(2000);
            SendBackToSignInPage();
            return;
        }
        Collapse(BrowserPage.browser);
        if (ClientId == "" || ClientSecret == "")
        {
            NavigateToPage(LoadingPageName);
            return;
        }
        ClickAllowButtonIfExists();
        if (IsOnRedirectPage())
        {
            GetSingleUseCode();
            await GetUserAccessToken();
            await AttemptToAuthorizeMembership();
        }
        else if (IsOnLoginPage()) Show(BrowserPage.browser);
        else if (!IsOnAuthorizePage()) SendBackToSignInPage();
    }
}