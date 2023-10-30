using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using RyggRunes.Client.Core;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;
using Rygg.Runes.Data.Core;
using System.Collections.Immutable;

namespace Rygg.Runes.Proxy
{
    public class MysticProxy : IChatGPTProxy
    {
        protected Uri BaseUri { get; }
        protected IPublicClientApplication ClientApplication { get; }
        protected string ApiScope { get; }
        protected string SignInSignOutPolicy { get; }
        public MysticProxy(IConfiguration config, IPublicClientApplication clientApplication)
        {
            BaseUri = new Uri(config["MysticAPI"] ?? throw new InvalidDataException());
            ClientApplication = clientApplication;
            ApiScope = config["MSGraphApi:Scopes"] ?? throw new InvalidDataException();
            SignInSignOutPolicy = config["AzureAD:SignUpSignInPolicyId"] ?? throw new InvalidDataException();

        }
        protected HttpClient Create()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = BaseUri;
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
        public async Task<string> GetReading(Rune[] runes, SpreadTypes spreadType, string message = "Tell me the future", CancellationToken token = default)
        {
            try
            {
                var accounts = (await ClientApplication.GetAccountsAsync(SignInSignOutPolicy)).ToList();
                
                var result = await ClientApplication.AcquireTokenSilent(new string[] { ApiScope }, accounts.First()).ExecuteAsync();
                using (var client = Create())
                {
                    client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", result.AccessToken);
                    var content = new StringContent(JsonSerializer.Serialize(new MysticRequest()
                    {
                        Runes = runes.Select(r => r.ToString()).ToArray(),
                        Question = message,
                        SpreadType = spreadType
                    }), System.Text.Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("Mystic", content, token);
                    if (response.IsSuccessStatusCode)
                        return await response.Content.ReadAsStringAsync(token);
                }
            }
            catch(Exception ex)
            {
                throw;
            }
            throw new InvalidOperationException();
        }
    }
}
