using Microsoft.Extensions.Configuration;
using OpenAI_API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Rygg.Runes.Data.Core;
using Rygg.Runes.Spreads;

namespace RyggRunes.Client.Core
{
    public interface IChatGPTProxy
    {
        Task<string> GetReading(PlacedRune[] runes, SpreadTypes spreadType, string message = "Tell me the future", CancellationToken token = default);
    }
    
    public class ChatGPTProxy : IChatGPTProxy
    {
        public ChatGPTProxy(IConfiguration config)
        {
            ApiKey = config["Watson:APIKey"] ?? throw new InvalidDataException();
            Uri = new Uri(config["Watson:Uri"] ?? throw new InvalidDataException());
            ModelId = config["Watson:ModelId"] ?? throw new InvalidDataException();
            ProjectId = config["Watson:ProjectId"] ?? throw new InvalidDataException();
            AccessTokenUri = new Uri(config["Watson:AcessTokenUri"] ?? throw new InvalidDataException());
        }
        protected string ApiKey { get; }
        protected Uri Uri { get; }
        protected Uri AccessTokenUri { get; }
        protected string ProjectId { get; }
        protected string ModelId { get; }
        protected class IBMResponse
        {
            public string access_token { get; set; } = null!;
        }
        protected class WatsonReponse
        {
            public class Result
            {
                public string generated_text { get; set; } = null!;
            }
            public Result[] results { get; set; } = null!;
        }
        protected async Task<HttpClient> Create(CancellationToken token)
        {
            HttpClient client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = AccessTokenUri,
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "urn:ibm:params:oauth:grant-type:apikey" },
                    { "apikey", ApiKey }
                })
            };
            //request.Headers.Clear();
            //request.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            var resp = await client.SendAsync(request);
            resp.EnsureSuccessStatusCode();
            var data = (await resp.Content.ReadFromJsonAsync<IBMResponse>()) ?? throw new InvalidOperationException();   
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", data.access_token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
        public async Task<string> GetReading(PlacedRune[] runes, SpreadTypes spreadType, string message = "Tell me the future", CancellationToken token = default)
        {
            var spread = SpreadFactory.Create(spreadType);
            spread.Validate(runes, out PlacedRune?[,] matrix);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"The following runes were cast using a spread of {spread.Name}:`");
            int j = 0, rowCount = matrix.GetLength(0), columnCount = matrix.GetLength(1);
            while(j < rowCount)
            {
                int i = 0;
                while(i < columnCount)
                {
                    PlacedRune? rune = matrix[j, i];
                    if (rune != null)
                        sb.AppendLine($"{rune.Name} @Position: [{j}, {i}]");
                    i++;
                }
                j++;
            }
            sb.AppendLine("`");
            using(var client = await Create(token))
            {
                var resp = await client.PostAsJsonAsync(Uri, new
                {
                    model_id = ModelId,
                    input = $"{sb} Based on those runes answer the question: {message}",
                    parameters= new {
                    decoding_method= "sample",
                      max_new_tokens= 900,
                        min_new_tokens= 100,
                      random_seed= null as int?,
                      stop_sequences= new string[] {},
                      temperature= 1.5,
                      top_k= 50,
                      top_p= 1,
                      repetition_penalty= 1
                    },
                    project_id= ProjectId
                }, token);
                resp.EnsureSuccessStatusCode();
                var str = await resp.Content.ReadFromJsonAsync<WatsonReponse>(cancellationToken: token);
                return str.results.First().generated_text;
            }

        }
    }
}
