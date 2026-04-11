namespace RedisSharp;

public static class RespParser
{
    /// <summary>
    /// Parses a RESP-encoded request into a list of string parts.
    /// Example: "*2\r\n$4\r\nECHO\r\n$3\r\nhey\r\n" => ["ECHO", "hey"]
    /// </summary>
    public static List<string> Parse(string request)
    {
        var lines = request.Split("\r\n");
        var parts = new List<string>();
        int i = 0;

        if (lines[i].StartsWith("*"))
        {
            int count = int.Parse(lines[i][1..]);
            i++;
            for (int j = 0; j < count; j++)
            {
                if (lines[i].StartsWith("$")) i++; // skip length line
                parts.Add(lines[i]);
                i++;
            }
        }
        return parts;

    }
}
