using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuacamoleClient.Common
{
    public sealed class GuacamoleUrlAndContentChecks
    {
        /// <summary>
        /// Is the value a valid URL with accepted scheme (http or https)?
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsValidUrlAndAcceptedScheme(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            if (Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
                return uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp;
            return false;
        }

        /// <summary>
        /// Determines whether the specified HTTP response content represents a Guacamole start page.
        /// NOTE: HTML content must be the raw HTML before any JavaScript execution.
        /// </summary>
        /// <param name="url">The url to the guacamole server</param>
        /// <returns>true if the content contains a Guacamole start page; otherwise, false.</returns>
        public static bool IsGuacamoleResponseWithStartPage(string? url, bool ignoreCertificateErrors)
        {
            if (url != null && Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            {
                var handler = new System.Net.Http.HttpClientHandler();
                if (ignoreCertificateErrors)
                {
                    handler.ServerCertificateCustomValidationCallback = (_, __, ___, ____) => true;
                }
                using var request = new System.Net.Http.HttpClient(handler);
                try
                {
                    var response = request.GetAsync(uri).Result;
                    if (!response.IsSuccessStatusCode)
                        return false;
                    var content = response.Content.ReadAsStringAsync().Result;
                    return ContentIsGuacamoleStartPage(content);
                }
                catch
                {
                    return false;
                }
            }
            else
                return false;
        }

        /// <summary>
        /// Determines whether the specified HTTP response content represents a Guacamole login form.
        /// NOTE: HTML content must be the dynamic HTML result after JavaScript execution.
        /// </summary>
        /// <param name="url">The url to the guacamole server</param>
        /// <returns>true if the content contains a Guacamole login form; otherwise, false.</returns>
        [Obsolete("This method may not work as expected because it does not execute JavaScript. Use ContentIsGuacamoleLoginForm with dynamic HTML content from browser instead.")]
        public static bool IsGuacamoleResponseWithLoginForm(string url, bool ignoreCertificateErrors)
        {
            if (url != null && Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            {
                var handler = new System.Net.Http.HttpClientHandler();
                if (ignoreCertificateErrors)
                {
                    handler.ServerCertificateCustomValidationCallback = (_, __, ___, ____) => true;
                }
                using var request = new System.Net.Http.HttpClient(handler);
                try
                {
                    var response = request.GetAsync(uri).Result;
                    if (!response.IsSuccessStatusCode)
                        return false;
                    var content = response.Content.ReadAsStringAsync().Result;
                    return ContentIsGuacamoleLoginForm(content);
                }
                catch
                {
                    return false;
                }
            }
            else
                return false;
        }

        /// <summary>
        /// Determines whether the specified HTML content represents a Guacamole login form.
        /// NOTE: HTML content must be the dynamic HTML result after JavaScript execution.
        /// </summary>
        /// <remarks>This method checks for specific HTML markers commonly found in Guacamole login forms,
        /// such as certain class and field names. It does not perform a full HTML parse and may return false positives
        /// if the content contains similar elements.</remarks>
        /// <param name="content">The HTML content to examine for the presence of a Guacamole login form. Cannot be null or whitespace.</param>
        /// <returns>true if the content contains the expected elements of a Guacamole login form; otherwise, false.</returns>
        public static bool ContentIsGuacamoleLoginForm(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;
            return content.Contains("class=\"login-fields\"") && content.Contains("id=\"guac-field-") && content.Contains("name=\"username\"") && content.Contains("name=\"password\"");
        }

        /// <summary>
        /// Determines whether the specified content represents a Guacamole start page.
        /// NOTE: HTML content must be the raw HTML before any JavaScript execution.
        /// </summary>
        /// <param name="content">The HTML content to examine for the presence of a Guacamole start page. Can be null or empty.</param>
        /// <returns>true if the content contains a Guacamole start page marker; otherwise, false.</returns>
        public static bool ContentIsGuacamoleStartPage(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;
            return content.Contains("<guac-modal>");
        }
    }
}
