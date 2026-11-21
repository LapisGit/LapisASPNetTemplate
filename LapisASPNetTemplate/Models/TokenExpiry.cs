using System.Text.Json.Serialization;

namespace LapisASPNetTemplate.Models;

public class TokenExpiry
{
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }

    // unix seconds, matches the jwt iat claim so expiry checks need no conversion
    [JsonPropertyName("expiredAtUnix")]
    public long ExpiredAtUnix { get; set; }
}