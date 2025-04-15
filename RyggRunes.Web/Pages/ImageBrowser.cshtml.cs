using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RyggRunes.Client.Core;

namespace RyggRunes.Web.Pages
{
    [Authorize]
    public class ImageBrowserModel : PageModel
    {
        public List<string> Files { get; } = new List<string>();
        protected IStorageBlob Storage { get; }
        public ImageBrowserModel(IStorageBlob storage)
        {
            Storage = storage;
        }
        public async Task OnGetAsync(CancellationToken token = default)
        {
            await foreach(var file in Storage.GetImageFiles(token))
            {
                Files.Add(file);
            }
        }
    }
}
