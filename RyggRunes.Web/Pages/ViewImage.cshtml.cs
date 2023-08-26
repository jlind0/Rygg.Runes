using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RyggRunes.Client.Core;

namespace RyggRunes.Web.Pages
{
    [Authorize]
    public class ViewImageModel : PageModel
    {
        public AnnotatedImage? ImageData { get; private set; }
        protected IStorageBlob Storage { get; }
        public ViewImageModel(IStorageBlob storage)
        {
            Storage = storage;
        }
        public async Task OnGetAsync(string fileName, CancellationToken token = default)
        {
            ImageData = await Storage.GetImage(fileName, token);
        }
    }
}
