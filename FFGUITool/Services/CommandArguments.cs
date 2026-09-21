using System;
using System.Collections.Generic;
using System.Text;

namespace FFGUITool.Services;

public static class CommandArguments
{
    public static string Quote(string value)
    {
        var result = new StringBuilder("\"");
        var slashes = 0;
        foreach (var character in value)
        {
            if (character == '\\') { slashes++; continue; }
            result.Append('\\', character == '"' ? slashes * 2 + 1 : slashes);
            result.Append(character);
            slashes = 0;
        }
        result.Append('\\', slashes * 2);
        return result.Append('"').ToString();
    }

    // Parses the application's display command into argv without invoking a shell.
    public static IReadOnlyList<string> Parse(string text)
    {
        var result = new List<string>();
        var value = new StringBuilder();
        var quoted = false;
        var started = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\\')
            {
                var count = 1;
                while (i + 1 < text.Length && text[i + 1] == '\\') { count++; i++; }
                if (i + 1 < text.Length && text[i + 1] == '"')
                {
                    value.Append('\\', count / 2);
                    if (count % 2 != 0) value.Append('"');
                    else quoted = !quoted;
                    i++;
                }
                else value.Append('\\', count);
                started = true;
            }
            else if (c == '"') { quoted = !quoted; started = true; }
            else if (char.IsWhiteSpace(c) && !quoted)
            { if (started) { result.Add(value.ToString()); value.Clear(); started = false; } }
            else { value.Append(c); started = true; }
        }
        if (quoted) throw new ArgumentException("Unclosed command argument.");
        if (started) result.Add(value.ToString());
        return result;
    }
}
