using System;
using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace MostlyArmless.ModuleShell;

public partial class MainWindow : Window
{
    private LocalModuleHost? _host;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += LoadLocalDisplayAsync;
    }

    private async void LoadLocalDisplayAsync(object sender, RoutedEventArgs e)
    {
        var uiRoot = Path.Combine(AppContext.BaseDirectory, "ui");
        var entryPoint = Path.Combine(uiRoot, "index.html");
        if (!File.Exists(entryPoint))
        {
            Browser.NavigateToString("<main style='font-family:Segoe UI;background:#070d10;color:#d8ffe9;padding:32px'><h1>Mostly Armless module</h1><p>The local display bundle is missing. Build the module web bundle before launch.</p></main>");
            return;
        }

        _host = new LocalModuleHost(uiRoot);
        var started = await _host.StartAsync();
        var webViewData = Path.Combine(AppContext.BaseDirectory, "data", "webview");
        Directory.CreateDirectory(webViewData);
        var environment = await CoreWebView2Environment.CreateAsync(null, webViewData);
        await Browser.EnsureCoreWebView2Async(environment);
        Browser.Source = started
            ? new Uri(_host.BaseAddress)
            : new Uri($"file:///{entryPoint.Replace('\\', '/')}");
    }

    protected override void OnClosed(EventArgs e)
    {
        _host?.Dispose();
        base.OnClosed(e);
    }
}
