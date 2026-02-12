using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Electra.Models;

public class UserProfileUpdateRequest
{
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }
        
    [JsonPropertyName("name")]
    public string Name { get; set; }
        
    [JsonPropertyName("website")]
    public string Website { get; set; }

    [JsonPropertyName("social_media")] 
    public Dictionary<string, string> SocialMedia { get; set; } = new();

//        [JsonPropertyName("firstname")] 
//        public string Firstname { get; set; }
//        
//        [JsonPropertyName("lastname")]
//        public string Lastname { get; set; }
        
    [JsonPropertyName("tagline")]
    public string Tagline { get; set; }
        
    [JsonPropertyName("location")]
    public string Location { get; set; }
        
    [JsonPropertyName("bio")]
    public string Bio { get; set; }
}