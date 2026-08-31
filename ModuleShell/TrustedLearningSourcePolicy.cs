using System.Net;

namespace MATeacher.ModuleShell;

internal static class TrustedLearningSourcePolicy
{
    private const int MaximumRedirects = 5;

    private static readonly HashSet<string> TrustedHostSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "gov.uk",
        "parliament.uk",
        "nhs.uk",
        "metoffice.gov.uk",
        "nationalarchives.gov.uk",
        "bl.uk",
        "britishmuseum.org",
        "royalsociety.org",
        "rsc.org",
        "stem.org.uk",
        "oaknationalacademy.uk",
        "educationendowmentfoundation.org.uk",
        "aqa.org.uk",
        "ocr.org.uk",
        "wjec.co.uk"
    };

    private static readonly HashSet<string> TrustedExactHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "qualifications.pearson.com"
    };

    internal static bool TryValidate(Uri? uri, out string reason)
    {
        if (uri is null || !uri.IsAbsoluteUri)
        {
            reason = "The learning source must be an absolute web address.";
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            reason = "Learning sources must use HTTPS.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(uri.UserInfo))
        {
            reason = "Learning-source addresses cannot contain a username or password.";
            return false;
        }

        if (!uri.IsDefaultPort || uri.Port != 443)
        {
            reason = "Learning sources must use the normal secure web port.";
            return false;
        }

        var host = uri.IdnHost.Trim().TrimEnd('.').ToLowerInvariant();
        if (host.Length == 0 || IPAddress.TryParse(host, out _))
        {
            reason = "Learning sources must use a named, approved organisation website.";
            return false;
        }

        if (IsTrustedHost(host) || IsTrustedEducationPath(host, uri.AbsolutePath))
        {
            reason = string.Empty;
            return true;
        }

        reason = $"{host} is not on MA-Teacher's official learning-source allowlist.";
        return false;
    }

    internal static async Task<HttpResponseMessage> GetAsync(
        HttpClient client,
        string address,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var current))
        {
            throw new InvalidOperationException("The learning-source address is invalid.");
        }

        for (var redirect = 0; redirect <= MaximumRedirects; redirect++)
        {
            EnsureTrusted(current);
            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!IsRedirect(response.StatusCode))
            {
                return response;
            }

            var location = response.Headers.Location;
            response.Dispose();
            if (location is null)
            {
                throw new InvalidOperationException("The learning source returned a redirect without a destination.");
            }

            current = location.IsAbsoluteUri ? location : new Uri(current, location);
        }

        throw new InvalidOperationException($"The learning source used more than {MaximumRedirects} redirects.");
    }

    private static void EnsureTrusted(Uri uri)
    {
        if (!TryValidate(uri, out var reason))
        {
            throw new InvalidOperationException(reason);
        }
    }

    private static bool IsTrustedHost(string host)
    {
        if (TrustedExactHosts.Contains(host))
        {
            return true;
        }

        return TrustedHostSuffixes.Any(suffix =>
            string.Equals(host, suffix, StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith('.' + suffix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsTrustedEducationPath(string host, string path)
    {
        return (string.Equals(host, "bbc.co.uk", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(host, "www.bbc.co.uk", StringComparison.OrdinalIgnoreCase)) &&
               (string.Equals(path, "/bitesize", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/bitesize/", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
    {
        var value = (int)statusCode;
        return value is 301 or 302 or 303 or 307 or 308;
    }
}
