namespace Aero.Social.Models;

public class PollDetails
{
    public List<string> Options { get; set; } = new();
    public int DurationHours { get; set; }
}
