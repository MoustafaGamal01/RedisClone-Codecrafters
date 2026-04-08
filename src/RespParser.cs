namespace RedisSharp;

public static class RespParser
{
    /// <summary>
    /// Parses a RESP-encoded request into a list of string parts.
    /// Example: "*2\r\n$4\r\nECHO\r\n$3\r\nhey\r\n" => ["ECHO", "hey"]
    /// </summary>
    public static List<string> Parse(string request)
    {
        var parts = new List<string>();
        var lines = request.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            // Skip array (*) and bulk string length ($) indicators
            if (line.StartsWith("*") || line.StartsWith("$"))
                continue;

            parts.Add(line);
        }

        return parts;
    }
}
