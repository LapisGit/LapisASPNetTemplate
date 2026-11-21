using System.Text.Json.Serialization;

namespace LapisASPNetTemplate.Models;

public class Account
{
    [JsonPropertyName("accountId")]
    public int AccountId { get; set; }
    
    [JsonPropertyName("accountName")]
    public string AccountName { get; set; }
    
    // hashed password, never stored in plain text lol
    [JsonPropertyName("password")]
    public string Password { get; set; }
    
    [JsonPropertyName("email")]
    public string Email { get; set; }
    
    [JsonPropertyName("bio")]
    public string Bio { get; set; }
    
    [JsonPropertyName("dateCreated")]
    public DateTime DateCreated { get; set; }
}