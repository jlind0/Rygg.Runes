using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using RyggRunes.Client.Core;
using Rygg.Runes.Data.Core;
using Rygg.Runes.Spreads;

namespace RyggRunes.Web.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MysticController : ControllerBase
    {
        protected IChatGPTProxy ChatProxy { get; }
        public MysticController(IChatGPTProxy chatProxy)
        {
            ChatProxy = chatProxy;
        }
        [HttpPost]
        public Task<string> AskQuestions([FromBody] MysticRequest request, CancellationToken token = default)
        {
            return ChatProxy.GetReading(request.Runes.Select(r => new PlacedRune(r)).ToArray(), 
                request.SpreadType, request.Question, token);
        }
    }
    
}
