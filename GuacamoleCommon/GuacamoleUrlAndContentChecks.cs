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
        /// </summary>
        /// <param name="url">The url to the guacamole server</param>
        /// <returns>true if the content contains a Guacamole start page; otherwise, false.</returns>
        public static bool IsGuacamoleResponseWithStartPage(string? url)
        {
            if (url != null && Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            {
                System.Net.Http.HttpClient request = new System.Net.Http.HttpClient();
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
        /// </summary>
        /// <param name="url">The url to the guacamole server</param>
        /// <returns>true if the content contains a Guacamole login form; otherwise, false.</returns>
        public static bool IsGuacamoleResponseWithLoginForm(string url)
        {
            if (url != null && Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            {
                System.Net.Http.HttpClient request = new System.Net.Http.HttpClient();
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

        public static bool ContentIsGuacamoleLoginForm(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;
            return content.Contains("class=\"login-fields\"") && content.Contains("id=\"guac-field-") && content.Contains("name=\"username\"") && content.Contains("name=\"password\"");
        }
        public static bool ContentIsGuacamoleStartPage(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;
            return content.Contains("<guac-modal>");
        }
    }
}
