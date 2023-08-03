using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace RyggRunes.Client.Core
{
    public interface IRunesProxy
    {
        Task<string[]> ProcessImage(byte[] imageBytes, CancellationToken token = default);
    }
    public class RunesPostResponse
    {
        public bool success { get; set; }
        public string[] annotations { get; set; }
    }
    public class RunesPostRequest
    {
        public string image { get; set; }
    }
    public class RunesProxy : IRunesProxy
    {
        protected Uri BaseUri { get; }
        public RunesProxy(IConfiguration config)
        {
            BaseUri = new Uri(config["RunesAPI:BaseUri"]);

        }
        protected HttpClient Create()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = BaseUri;
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
        public async Task<string[]?> ProcessImage(byte[] imageBytes, CancellationToken token = default)
        {
            try
            {
                RunesPostResponse? resp = null;
                using (var client = Create())
                {
                    using(var content = new ByteArrayContent(imageBytes))
                    {
                        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                        var response = await client.PostAsync("process_image/", content, token);
                        if (response.IsSuccessStatusCode)
                            resp = await response.Content.ReadFromJsonAsync<RunesPostResponse>(cancellationToken: token);
                    }
                    

                    
                }
                return resp?.annotations;
            }
            catch(Exception ex)
            {
                throw;
            }
        }
    }
}
