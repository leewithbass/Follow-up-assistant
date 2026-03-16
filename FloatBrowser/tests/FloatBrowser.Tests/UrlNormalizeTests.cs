using FloatBrowser.App.ViewModels;

namespace FloatBrowser.Tests;

public class UrlNormalizeTests
{
    [Fact]
    public void NormalizeUrl_AddsHttps_WhenDomainOnly()
    {
        var url = MainWindowViewModel.NormalizeUrl("example.com");
        Assert.Equal("https://example.com", url);
    }

    [Fact]
    public void NormalizeUrl_ReturnsNull_WhenInvalid()
    {
        var url = MainWindowViewModel.NormalizeUrl("not a url");
        Assert.Null(url);
    }
}
