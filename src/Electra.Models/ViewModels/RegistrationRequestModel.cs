using System;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

namespace Electra.Models.ViewModels
{
    public record RegistrationRequestModel 
    {
        [JsonPropertyName("username")]
        public string Username { get; set; }
        
        [JsonPropertyName("email")]
        public string Email { get; set; }
        
        [JsonPropertyName("firstname")]
        public string Firstname { get; set; }
        
        [JsonPropertyName("lastname")]
        public string Lastname { get; set; }
        
        [JsonPropertyName("password")]
        public string Password { get; set; }
        
        [JsonPropertyName("confirmed_password")]
        public string ConfirmedPassword { get; set; }
        
        [PersonalData]
        [JsonPropertyName("birthday")]
        public DateTime? Birthday { get; set; }
        
        [PersonalData]
        [JsonPropertyName("mobile_number")]
        public string MobileNumber { get; set; }
        
        [JsonPropertyName("postal_code")]
        public string PostalCode { get; set; }
        
        [JsonPropertyName("country")]
        public string Country { get; set; }
        
        [JsonPropertyName("agreed_tos")]
        public bool AgreedToTos { get; set; }

        [JsonPropertyName("address")]
        public string Address { get; set; }
    }
}