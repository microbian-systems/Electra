using Electra.Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace Electra.Models;

public record AddressModel : Entity
{
    public string UserId { get; set; }
    [PersonalData]
    public string AddressLine1 { get; set; }
    [PersonalData]
    public string AddressLine2 { get; set; }
    [PersonalData]
    public string AddressLine3 { get; set; }
    [PersonalData]
    public string City { get; set; }
    public string State { get; set; }
    public string StateCode { get; set; }
    public string Country { get; set; }
    public string CountryCode { get; set; }
    public string PostalCode { get; set; }
    public bool IsMain { get; set; }
    public bool IsActive { get; set; }
    [PersonalData]
    public double? Latitude { get; set; }
    [PersonalData]
    public double? Longitude { get; set; }
}