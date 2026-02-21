using Aero.CMS.Core.Content.Models;

namespace Aero.CMS.Components.Admin.PageSection;

public partial class PageEditor
{
    [Parameter] public string PageId { get; set; } = string.Empty;

    private ContentDocument? _page;
    private bool _saving;
    private bool _saved;
    private CancellationTokenSource? _debounce;
    
    [Inject] private ILogger<PageEditor> log { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        log.LogInformation("PageEditor: OnInitializedAsync for PageId: {PageId}", PageId);

        if (Guid.TryParse(PageId, out var guid))
        {
            log.LogInformation("PageEditor: Loading page by GUID: {Guid}", guid);
            _page = await PageService.GetByIdAsync(guid);
        }
        else
        {
            log.LogWarning("PageEditor: PageId {PageId} is not a valid GUID. Attempting secondary parse...", PageId);
            var lastPart = PageId.Split('/').Last();
            if (Guid.TryParse(lastPart, out var partGuid))
            {
                log.LogInformation("PageEditor: Loading page by secondary GUID: {Guid}", partGuid);
                _page = await PageService.GetByIdAsync(partGuid);
            }
        }

        if (_page is null)
        {
            log.LogError("PageEditor: Page not found for {PageId}. Redirecting to /admin.", PageId);
            Nav.NavigateTo("/admin");
        }
        else
        {
            log.LogInformation("PageEditor: Successfully loaded page {PageName}", _page.Name);
        }
    }

    private void ScheduleSave()
    {
        _saved = false;
        _debounce?.Cancel();
        _debounce = new CancellationTokenSource();
        var token = _debounce.Token;
        
        // Execute on thread pool to avoid blocking UI
        Task.Run(async () => {
            try 
            {
                await Task.Delay(800, token);
                if (!token.IsCancellationRequested)
                {
                    await InvokeAsync(SaveNow);
                }
            }
            catch (TaskCanceledException) { }
        });
    }

    private async Task SaveNow()
    {
        if (_page is null) return;
        _saving = true;
        _saved = false;
        StateHasChanged();
        await PageService.SavePageAsync(_page, "admin");
        _saving = false;
        _saved = true;
        StateHasChanged();
        await Task.Delay(2000);
        _saved = false;
        StateHasChanged();
    }

    public ValueTask DisposeAsync()
    {
        _debounce?.Cancel();
        _debounce?.Dispose();
        return ValueTask.CompletedTask;
    }
}