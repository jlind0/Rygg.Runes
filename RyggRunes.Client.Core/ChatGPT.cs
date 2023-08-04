using Microsoft.Extensions.Configuration;
using OpenAI_API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RyggRunes.Client.Core
{
    public interface IChatGPTProxy
    {
        Task<string> GetReading(string[] runes, string message = "Tell me the future", CancellationToken token = default);
    }
    public class ChatGPTProxy : IChatGPTProxy
    {
        public ChatGPTProxy(IConfiguration config)
        {
            ApiKey = config["ChatGPT:APIKey"];
            SystemPrompt = config["ChatGPT:SystemPrompt"];
            OrgId = config["ChatGPT:OrgId"];
        }
        protected string ApiKey { get; }
        protected string SystemPrompt { get; }
        protected string OrgId { get; }
        protected OpenAIAPI Create()
        {
            return new OpenAIAPI(new APIAuthentication(ApiKey, OrgId));
        }
        public async Task<string> GetReading(string[] runes, string message = "Tell me the future", CancellationToken token = default)
        {
            var castRunes = string.Join(",", runes.Select(r => r.Split(' ').First()));
            message = $"The following runes were cast: {castRunes}. {message}";
            var api = Create();
            var chat = api.Chat.CreateConversation();
            chat.AppendSystemMessage(SystemPrompt);
            chat.AppendUserInput(message);
            return await chat.GetResponseFromChatbotAsync();

        }
    }
}
