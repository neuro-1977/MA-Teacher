using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MostlyArmless.ModuleShell;

/// <summary>
/// Keeps a module self-contained: the desktop shell and a bookmarkable local URL
/// are two views of the same static bundle. This is not a Windows service.
/// </summary>
internal sealed class LocalModuleHost : IDisposable
{
    private static readonly IReadOnlyDictionary<string, ModuleIdentity> KnownModules =
        new Dictionary<string, ModuleIdentity>(StringComparer.OrdinalIgnoreCase)
        {
            ["MA-Dev"] = new("uk.mostlyarmless.dev", "MA-Dev", 5200),
            ["MA-Teacher"] = new("uk.mostlyarmless.teacher", "MA-Teacher", 5201),
            ["Ma-Bots"] = new("uk.mostlyarmless.bots", "MA-Bots", 5202),
            ["MA-Research"] = new("uk.mostlyarmless.research", "MA-Research", 5203),
            ["MA-SmartEdit"] = new("uk.mostlyarmless.smartedit", "MA-SmartEdit", 5204),
            ["MA-SrcCtrl"] = new("uk.mostlyarmless.srcctrl", "MA-SrcCtrl", 5205),
            ["MA-Stream"] = new("uk.mostlyarmless.stream", "MA-Stream", 5206),
        };

    private readonly HttpListener _listener = new();
    private readonly string _uiRoot;
    private readonly ModuleIdentity _identity;
    private readonly CancellationTokenSource _stopping = new();
    private Task? _serveTask;

    public LocalModuleHost(string uiRoot)
    {
        _uiRoot = Path.GetFullPath(uiRoot);
        _identity = ResolveIdentity();
        BaseAddress = $"http://127.0.0.1:{_identity.Port}/";
        _listener.Prefixes.Add(BaseAddress);
    }

    public string BaseAddress { get; }

    public Task<bool> StartAsync()
    {
        try
        {
            _listener.Start();
            _serveTask = Task.Run(ServeAsync);
            return Task.FromResult(true);
        }
        catch (HttpListenerException)
        {
            return Task.FromResult(false);
        }
    }

    private async Task ServeAsync()
    {
        while (!_stopping.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (HttpListenerException) when (_stopping.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            _ = Task.Run(() => WriteResponseAsync(context));
        }
    }

    private async Task WriteResponseAsync(HttpListenerContext context)
    {
        try
        {
            var path = Uri.UnescapeDataString(context.Request.Url?.AbsolutePath ?? "/");
            if (string.Equals(path, "/ma-id", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                await WriteTextAsync(context.Response, JsonSerializer.Serialize(new
                {
                    id = _identity.Id,
                    name = _identity.Name,
                    port = _identity.Port,
                }));
                return;
            }

            var relative = path == "/" ? "index.html" : path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var candidate = Path.GetFullPath(Path.Combine(_uiRoot, relative));
            if (!candidate.StartsWith(_uiRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(candidate))
            {
                candidate = Path.Combine(_uiRoot, "index.html");
            }

            context.Response.ContentType = GetMimeType(candidate);
            context.Response.ContentLength64 = new FileInfo(candidate).Length;
            await using var input = File.OpenRead(candidate);
            await input.CopyToAsync(context.Response.OutputStream, _stopping.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            if (context.Response.OutputStream.CanWrite)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        }
        finally
        {
            context.Response.Close();
        }
    }

    private static async Task WriteTextAsync(HttpListenerResponse response, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
    }

    private static ModuleIdentity ResolveIdentity()
    {
        var moduleFolder = Assembly.GetEntryAssembly()
            ?.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(attribute.Key, "MostlyArmless.ModuleFolder", StringComparison.Ordinal))
            ?.Value;
        if (!string.IsNullOrWhiteSpace(moduleFolder) && KnownModules.TryGetValue(moduleFolder, out var packagedIdentity))
        {
            return packagedIdentity;
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (string.Equals(directory.Name, "ModuleShell", StringComparison.OrdinalIgnoreCase)
                && directory.Parent is not null
                && KnownModules.TryGetValue(directory.Parent.Name, out var identity))
            {
                return identity;
            }

            directory = directory.Parent;
        }

        return new ModuleIdentity("uk.mostlyarmless.module", "Mostly Armless Module", 5207);
    }

    private static string GetMimeType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".html" => "text/html; charset=utf-8",
        ".js" => "text/javascript; charset=utf-8",
        ".css" => "text/css; charset=utf-8",
        ".json" => "application/json; charset=utf-8",
        ".svg" => "image/svg+xml",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        ".ico" => "image/x-icon",
        _ => "application/octet-stream",
    };

    public void Dispose()
    {
        _stopping.Cancel();
        if (_listener.IsListening)
        {
            _listener.Stop();
        }

        _listener.Close();
        _stopping.Dispose();
    }

    private sealed record ModuleIdentity(string Id, string Name, int Port);
}
