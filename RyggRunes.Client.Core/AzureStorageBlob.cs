using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RyggRunes.Client.Core
{
    public class AnnotatedImage
    {
        public byte[] im_incomming { get; set; } = null!;
        public string[] annotations { get; set; } = null!;
        public byte[] im_annotated { get; set; } = null!;
    }
    public interface IStorageBlob
    {
        IAsyncEnumerable<string> GetImageFiles([EnumeratorCancellation] CancellationToken token = default);
        Task<AnnotatedImage?> GetImage(string name, CancellationToken token = default);
    }
    public class StorageBlob : IStorageBlob
    {
        protected string ConnectionString { get; }
        protected string ContainerName { get; }
        public StorageBlob(IConfiguration config)
        {
            ConnectionString = config["AzureBlob:ConnectionString"] ?? throw new InvalidDataException();
            ContainerName = config["AzureBlob:Container"] ?? throw new InvalidDataException();
        }

        public async IAsyncEnumerable<string> GetImageFiles([EnumeratorCancellation]CancellationToken token = default)
        {
            var client = CreateClient();
            await foreach(var blog in client.GetBlobsAsync(cancellationToken: token))
            {
                yield return blog.Name;
            }

        }

        public async Task<AnnotatedImage?> GetImage(string name, CancellationToken token = default)
        {
            var client = CreateClient();
            var reader = client.GetBlobClient(name);
            if(await reader.ExistsAsync(token))
            {
                using (BlobDownloadInfo dwld = await reader.DownloadAsync())
                {
                    string jsonContent;
                    using (var streamReader = new StreamReader(dwld.Content))
                    {
                        jsonContent = await streamReader.ReadToEndAsync();
                    }
                    return JsonSerializer.Deserialize<AnnotatedImage>(jsonContent ?? throw new InvalidDataException());
                }
            }
            return null;
        }
        protected BlobContainerClient CreateClient()
        {
            var blobServiceClient = new BlobServiceClient(ConnectionString);
            return blobServiceClient.GetBlobContainerClient(ContainerName);
        }
    }
}
