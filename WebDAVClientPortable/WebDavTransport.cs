using System;
using System.Net.Http;

namespace WebDAVClient
{
    public static class WebDavTransport
    {
        public static Uri RequireHttps(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps || string.IsNullOrEmpty(uri.Host) ||
                !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment))
                throw new ArgumentException("WebDAV requires an HTTPS address without embedded credentials or a fragment.");
            return uri;
        }

        public static Uri RequireSameOrigin(string value, Uri origin)
        {
            var uri = RequireHttps(value);
            if (uri.IdnHost != origin.IdnHost || uri.Port != origin.Port || origin.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("The WebDAV login endpoint must use the configured HTTPS server.");
            return uri;
        }

        public static HttpClient CreateLoginClient() => new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
    }
}
