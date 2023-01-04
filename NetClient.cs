using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ZModLauncher;

public class NetClient
{
    public static readonly HttpClient Client = new();
    public string RequestContent;
    public string RequestFormat;
    public string Url;

    public NetClient() { }

    public NetClient(string headerName, string headerValue)
    {
        Client.DefaultRequestHeaders.Remove(headerName);
        Client.DefaultRequestHeaders.Add(headerName, headerValue);
    }

    private static async Task<string> SerializeString(HttpResponseMessage response)
    {
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<T> Serialize<T>(HttpResponseMessage response)
    {
        return JsonConvert.DeserializeObject<T>(await SerializeString(response));
    }

    public async Task<HttpResponseMessage> GET()
    {
        return await Client.GetAsync(Url);
    }

    public async Task<HttpResponseMessage> POST()
    {
        var content = new StringContent(RequestContent, Encoding.UTF8, RequestFormat);
        return await Client.PostAsync(Url, content);
    }

    public async Task<T> GET<T>()
    {
        return await Serialize<T>(await GET());
    }

    public async Task<T> POST<T>()
    {
        return await Serialize<T>(await POST());
    }
}