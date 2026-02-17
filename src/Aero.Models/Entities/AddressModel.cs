using System.ComponentModel.DataAnnotations;
using Aero.Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace Aero.Models.Entities;

public class AddressModel : Entity
{
    [MaxLength(256)]
    public string UserId { get; set; }
    [PersonalData]
    [MaxLength(128)]
    public string AddressLine1 { get; set; }
    [PersonalData]
    [MaxLength(128)]
    public string AddressLine2 { get; set; }
    [PersonalData]
    [MaxLength(128)]
    public string AddressLine3 { get; set; }
    [PersonalData]
    [MaxLength(128)]
    public string City { get; set; }
    [MaxLength(128)]
    public string State { get; set; }
    public string StateCode { get; set; }
    [MaxLength(128)]
    public string Country { get; set; }
    [MaxLength(5)]
    public string CountryCode { get; set; }
    [MaxLength(128)]
    public string PostalCode { get; set; }
    public bool IsMain { get; set; }
    public bool IsActive { get; set; }
    [PersonalData]
    public double? Latitude { get; set; }
    [PersonalData]
    public double? Longitude { get; set; }
}