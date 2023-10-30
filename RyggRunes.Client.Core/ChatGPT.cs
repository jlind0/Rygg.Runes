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
        Task<string> GetReading(Rune[] runes, SpreadTypes spreadType, string message = "Tell me the future", CancellationToken token = default);
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
        public async Task<string> GetReading(Rune[] runes, SpreadTypes spreadType, string message = "Tell me the future", CancellationToken token = default)
        {
            var spread = SpreadFactory.Create(spreadType);
            spread.Validate(runes, out Rune?[,] matrix);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"The following runes were case using a spread of {spread.Name}:`");
            int j = 0, rowCount = matrix.GetLength(0), columnCount = matrix.GetLength(1);
            while(j < rowCount)
            {
                int i = 0;
                while(i < columnCount)
                {
                    Rune? rune = matrix[i, j];
                    if (rune != null)
                        sb.AppendLine($"{rune.Name} @Position: [{j}, {i}]");
                    i++;
                }
                j++;
            }
            sb.AppendLine("`");
            sb.AppendLine($"Asking the universe the following question:{message}");
            var api = Create();
            var chat = api.Chat.CreateConversation();
            chat.AppendSystemMessage(SystemPrompt);
            chat.AppendUserInput(sb.ToString());
            return await chat.GetResponseFromChatbotAsync();

        }
    }
}
