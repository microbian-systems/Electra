namespace Electra.Models.Geo;

public record GeoPoint(double Latitude, double Longitude)
{
    public double Latitude { get; } = Latitude;
    public double Longitude { get; } = Longitude;
}