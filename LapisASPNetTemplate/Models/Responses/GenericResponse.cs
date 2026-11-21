using System.Text.Json.Serialization;

namespace LapisASPNetTemplate.Models.Responses;

public class GenericResponse
{
    [JsonPropertyName("errorCode")]
    public int ErrorCode { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; }
}