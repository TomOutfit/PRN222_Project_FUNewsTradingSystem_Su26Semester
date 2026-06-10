using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FUNewsTradingSystem_DataAccessLayer.Models.DTOs
{
    public class OpenAiResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAiChoice> Choices { get; set; } = new();
    }

    public class OpenAiChoice
    {
        [JsonPropertyName("message")]
        public OpenAiMessage Message { get; set; } = null!;
    }
}
