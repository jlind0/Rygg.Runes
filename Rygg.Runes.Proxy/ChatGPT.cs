using MAUI.MSALClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using RyggRunes.Client.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Rygg.Runes.Proxy
{
    public class MysticProxy : IChatGPTProxy
    {
        protected Uri BaseUri { get; }
        protected IPublicClientApplication ClientApplication { get; }
        protected string ApiScope { get; }
        public MysticProxy(IConfiguration config, IPublicClientApplication clientApplication)
        {
            BaseUri = new Uri(config["MysticAPI"]);
            ClientApplication = clientApplication;
            ApiScope = config["AzureAD:ApiScope"];

        }
        protected HttpClient Create()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = BaseUri;
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
        protected class MysticRequest
        {
            public string[] Runes { get; set; }
            public string Question { get; set; }
        }
        public async Task<string> GetReading(string[] runes, string message = "Tell me the future", CancellationToken token = default)
        {
            var accounts = (await ClientApplication.GetAccountsAsync()).ToList();
            var result = await ClientApplication.AcquireTokenSilent(new string[] { "api://5991da6c-f355-4684-8048-aa9553b17fdd/access_as_user" }, accounts.First()).ExecuteAsync();
            using (var client = Create())
            {
                client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", result.AccessToken);
                var content = new StringContent(JsonSerializer.Serialize(new MysticRequest()
                {
                    Runes = runes,
                    Question = message
                }), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("Mystic", content, token);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsStringAsync(token);
            }
            throw new InvalidOperationException();
        }
    }
}
