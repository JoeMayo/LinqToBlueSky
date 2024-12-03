using LinqToBlueSky.Common;

using System.Net;
using System.Text.Json;
using System.Xml.Linq;

namespace LinqToBlueSky.Net;

public class BlueSkyErrorHandler
{
    // TODO: The versioning was specific to Twitter when they made the big change from v1 to v2. It probably doesn't apply to BlueSky.
    const int BlueSkyApiV1 = 1;
    const int BlueSkyApiV2 = 2;

    public static async Task ThrowIfErrorAsync(HttpResponseMessage msg)
    {
        const int TooManyRequests = 429;

        if ((int)msg.StatusCode < 400) return;

        switch (msg.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
                await HandleUnauthorizedAsync(msg).ConfigureAwait(false);
                break;
            default:
                switch ((int)msg.StatusCode)
                {
                    case TooManyRequests:
                        await HandleTooManyRequestsAsync(msg).ConfigureAwait(false);
                        break;
                    default:
                        await HandleGenericErrorAsync(msg).ConfigureAwait(false);
                        break;
                }
                break;
        }
    }

    internal static async Task HandleGenericErrorAsync(HttpResponseMessage msg)
    {
        string responseStr = await msg.Content.ReadAsStringAsync().ConfigureAwait(false);

        BuildAndThrowBlueSkyQueryException(responseStr, msg);
    }

    internal static async Task HandleTooManyRequestsAsync(HttpResponseMessage msg)
    {
        string responseStr = await msg.Content.ReadAsStringAsync().ConfigureAwait(false);

        BlueSkyErrorDetails error = ParseBlueSkyErrorMessage(responseStr);

        string message = error.Detail + " - Please visit the LINQ to Twitter FAQ (at the HelpLink) for help on resolving this error.";

        throw new BlueSkyQueryException(message)
        {
            HelpLink = L2BSKeys.FaqHelpUrl,
            StatusCode = HttpStatusCode.TooManyRequests,
            ReasonPhrase = msg.ReasonPhrase + " (HTTP 429 - Too Many Requests)",
            Title = error.Title,
            Details = error.Detail,
            Type = error.Type,
            Errors = error.Errors
        };
    }

    internal static void BuildAndThrowBlueSkyQueryException(string responseStr, HttpResponseMessage msg)
    {
        BlueSkyErrorDetails error = ParseBlueSkyErrorMessage(responseStr);

        string title = error?.Title ?? string.Empty;

        throw new BlueSkyQueryException(title)
        {
            StatusCode = msg.StatusCode,
            ReasonPhrase = msg.ReasonPhrase,
            Title = title,
            Details = error?.Detail ?? string.Empty,
            Type = error?.Type ?? string.Empty,
            Errors = error?.Errors ?? new List<Error>()
        };
    }

    internal async static Task HandleUnauthorizedAsync(HttpResponseMessage msg)
    {
        string responseStr = await msg.Content.ReadAsStringAsync().ConfigureAwait(false);

        BlueSkyErrorDetails error = ParseBlueSkyErrorMessage(responseStr);

        string message = error.Detail + " - Please visit the LINQ to Twitter FAQ (at the HelpLink) for help on resolving this error.";

        throw new BlueSkyQueryException(message)
        {
            HelpLink = L2BSKeys.FaqHelpUrl,
            StatusCode = HttpStatusCode.Unauthorized,
            ReasonPhrase = msg.ReasonPhrase,
            Title = error.Title,
            Details = error.Detail,
            Type = error.Type,
            Errors = error.Errors
        };
    }

    public static BlueSkyErrorDetails ParseBlueSkyErrorMessage(string responseStr)
    {
        try
        {
            bool isXml = responseStr.StartsWith("<?xml") || responseStr.StartsWith("<!DOCTYPE");

            if (isXml)
            {
                XElement responseElement = XElement.Parse(responseStr);

                XElement? errorElement =
                    responseElement
                        ?.Descendants("error")
                        ?.SingleOrDefault();

                if (errorElement == null)
                    return new BlueSkyErrorDetails
                    {
                        Detail = responseStr,
                        Title = "Unable to Parse Response - please examine Detail property",
                        Type = "XML Formatted Error"
                    };
                else
                    return new BlueSkyErrorDetails
                    {
                        Detail = responseStr,
                        Errors = new()
                        {
                            new()
                            {
                                Code = int.TryParse(errorElement.Attribute("code")?.Value, out int code) ? code : 0,
                                Message = errorElement.Value
                            }
                        }
                    };
            }

            var responseJson = JsonDocument.Parse(responseStr);
            var root = responseJson.RootElement;

            int apiVersion = GetBlueSkyApiVersion(root);

            if (apiVersion == BlueSkyApiV2) // version 2
            {
                List<Error>? errors = null;
                if (root.TryGetProperty("errors", out JsonElement errorElement))
                {
                    errors =
                        (from error in errorElement.EnumerateArray()
                         select new Error
                         {
                             Message = error.GetString("message"),
                             Parameters = GetErrorParameters(error)
                         })
                        .ToList();

                    return new BlueSkyErrorDetails
                    {
                        Title = root.GetString("title"),
                        Detail = root.GetString("detail"),
                        Type = root.GetString("type"),
                        Errors = errors
                    };
                }
                else if(root.TryGetProperty("error", out JsonElement errorMessage))
                {
                    root.TryGetProperty("error_description", out JsonElement errorDescription);

                    return new BlueSkyErrorDetails
                    {
                        Title = errorMessage.GetString(),
                        Detail = errorDescription.GetString(),
                        Type = "OAuth2",
                        Errors = new()
                        {
                            new Error
                            {
                                Code = 0,
                                Message = errorDescription.GetString(),
                                Request = errorMessage.GetString()
                            }
                        }
                    };
                }
                else if (root.TryGetProperty("detail", out JsonElement detail))
                {
                    root.TryGetProperty("title", out JsonElement title);
                    root.TryGetProperty("type", out JsonElement type);
                    root.TryGetProperty("status", out JsonElement status);

                    return new BlueSkyErrorDetails
                    {
                        Detail = detail.GetString(),
                        Errors = new()
                        {
                            new Error
                            {
                                Code = status.GetInt32(),
                                Message = title.GetString(),
                                Request = detail.GetString(),
                            }
                        },
                        Status = status.GetInt32(),
                        Title = title.GetString(),
                        Type = type.GetString()
                    };
                }
                else
                {
                    return new BlueSkyErrorDetails
                    {
                        Title = "Unknown Error",
                        Detail = root.GetString(),
                        Type = "Unknown",
                        Errors = errors
                    };
                }
            }
            else // version 1
            {
                if (root.TryGetProperty("request", out JsonElement discard))
                {
                    return new BlueSkyErrorDetails
                    {
                        Errors = new List<Error>
                        {
                            new Error
                            {
                                Request = root.GetString("request"),
                                Message = root.GetString("error")
                            }
                        }
                    };
                }
                else
                {
                    return new BlueSkyErrorDetails
                    {
                        Errors =
                            (from error in root.GetProperty("errors").EnumerateArray()
                             select new Error
                             {
                                 Message = error.GetString("message"),
                                 Code = error.GetInt("code")
                             })
                            .ToList()
                    };
                }

            }
        }
        catch (Exception)
        {
            return new BlueSkyErrorDetails 
            { 
                Title = 
                    "Unhandled Error - LINQ to BlueSky wasn't able to parse BlueSky error message. " +
                    "Please copy this message, with the Detail property contents and the query you " +
                    "were using (how to reproduce) to Joe Mayo at https://github.com/JoeMayo/LinqToBlueSky/issues.",
                Detail = responseStr 
            };
        }

    }

    static Dictionary<string, string[]> GetErrorParameters(JsonElement error)
    {
        if (error.TryGetProperty("parameters", out JsonElement paramElement))
            return
               (from parm in paramElement.EnumerateObject()
                let vals =
                  (from val in parm.Value.EnumerateArray()
                   select val.GetString())
                  .ToArray()
                select new { parm.Name, vals })
               .ToDictionary(
                    key => key.Name,
                    val => val.vals);
        else
            return new();
    }

    // TODO: This is likely not needed for BlueSky.
    static int GetBlueSkyApiVersion(JsonElement root)
    {
        bool hasError = root.TryGetProperty("error", out _);
        bool hasErrorDescription = root.TryGetProperty("error_description", out _);
        bool hasTitle = root.TryGetProperty("title", out _);
        bool hasType = root.TryGetProperty("type", out _);

        return 
            (hasTitle && hasType) || (hasError && hasErrorDescription) ? BlueSkyApiV2 : BlueSkyApiV1;
    }
}
