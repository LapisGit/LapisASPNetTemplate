using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LapisASPNetTemplate.Models.Requests;

public class LoginAccount
{
    [JsonPropertyName("username")]
    [Required]
    public string Username { get; set; }
    
    [JsonPropertyName("password")]
    [Required]
    public string Password { get; set; }
    
    [JsonPropertyName("platform")]
    [Required]
    public string Platform { get; set; }
}