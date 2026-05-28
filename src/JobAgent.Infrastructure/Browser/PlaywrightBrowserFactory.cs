using Microsoft.Playwright;

namespace JobAgent.Infrastructure.Browser;

public class PlaywrightBrowserFactory : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser != null)
            return _browser;

        await _lock.WaitAsync();
        try
        {
            if (_browser != null)
                return _browser;

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            });

            return _browser;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IPage> NewPageAsync()
    {
        var browser = await GetBrowserAsync();
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        });
        return await context.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.DisposeAsync();
            _browser = null;
        }
        _playwright?.Dispose();
        _playwright = null;
    }
}
