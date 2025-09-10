namespace Electra.Models.ViewModels;

public abstract record ServiceRequestResult<T>
{
    public bool Success { get; set; }
    public virtual T Result { get; set; }
    public HashSet<string> Errors { get; set; } = new();
    public HashSet<string> ValidationErrors { get; set; } = new();
}