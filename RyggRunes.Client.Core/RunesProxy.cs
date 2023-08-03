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
using System.Reflection.Emit;

namespace RyggRunes.Client.Core
{
    public interface IRunesProxy
    {
        Task<RunesPostResponse?> ProcessImage(byte[] imageBytes, CancellationToken token = default);
    }
    public class RunesPostResponse
    {
        public bool Success { get; set; }
        public string[] Annotations { get; set; }
        public byte[] AnnotatedImage { get; set; }
    }
    public class RunesPostRequest
    {
        public string image { get; set; }
    }
    public class RunesProxy : IRunesProxy
    {
        protected class RunesResponseRaw
        {
            public bool success { get; set; }
            public string[] annotations { get; set; }
            public string annotatedImage { get; set; }
        }
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
        public async Task<RunesPostResponse?> ProcessImage(byte[] imageBytes, CancellationToken token = default)
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
                        {
                            var raw = await response.Content.ReadFromJsonAsync<RunesResponseRaw>(cancellationToken: token);
                            if (raw != null)
                            {
                                resp = new RunesPostResponse();
                                resp.Annotations = raw.annotations;
                                resp.Success = raw.success;
                                resp.AnnotatedImage = Convert.FromBase64String(raw.annotatedImage);
                            }

                        }
                    }
                    

                    
                }
                return resp;
            }
            catch(Exception ex)
            {
                throw;
            }
        }
    }
}
