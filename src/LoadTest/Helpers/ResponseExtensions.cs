
using PuppeteerSharp;

namespace LoadTest.Helpers;

public static class ResponseExtensions
{
    public static void EnsureSuccessStatusCode(this IResponse response)
    {
        // Mimic HttpResponseMessage.EnsureSuccessStatusCode()
        if ((int)response.Status is not (>= 200 and <= 299))
        {
            throw new HttpRequestException($"Response status code does not indicate success: {(int)response.Status} ({response.Status}).");
        }
    }

    public static bool IsSuccessStatusCode(this IResponse response)
    {
        // Mimic HttpResponseMessage.EnsureSuccessStatusCode()
        return (int)response.Status is >= 200 and <= 299;
    }

    public static bool IsSuccessStatusCode(this HttpResponseMessage response)
    {
        // Mimic HttpResponseMessage.EnsureSuccessStatusCode()
        return (int)response.StatusCode is >= 200 and <= 299;
    }
}
