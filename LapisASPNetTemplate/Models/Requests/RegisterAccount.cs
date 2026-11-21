using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LapisASPNetTemplate.Models.Requests;

public class RegisterAccount
{
    [JsonPropertyName("username")]
    [Required]
    public string Username { get; set; }
    
    [JsonPropertyName("password")]
    [Required]
    public string Password { get; set; }
    
    [JsonPropertyName("email")]
    public string Email { get; set; }
}