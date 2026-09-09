using System;
using System.Reflection;
using System.Text;

// PROJECT 1864 v00.00.13k1
// Minimal JSON compatibility reader for the optional provider.json file.
// This avoids depending on Unity's JsonUtility module for the live-map provider config.
// It intentionally supports only string fields, which is all ProviderConfig requires.
public static class JsonUtility
{
    public static T FromJson<T>(string json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        object instance;
        try
        {
            instance = Activator.CreateInstance(typeof(T), true);
        }
        catch
        {
            return null;
        }

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        FieldInfo[] fields = typeof(T).GetFields(flags);
        foreach (FieldInfo field in fields)
        {
            if (field.FieldType != typeof(string))
                continue;

            string value;
            if (TryReadJsonString(json, field.Name, out value))
                field.SetValue(instance, value);
        }

        return instance as T;
    }

    private static bool TryReadJsonString(string json, string key, out string value)
    {
        value = null;
        string quotedKey = "\"" + key + "\"";
        int keyIndex = json.IndexOf(quotedKey, StringComparison.OrdinalIgnoreCase);
        if (keyIndex < 0)
            return false;

        int colon = json.IndexOf(':', keyIndex + quotedKey.Length);
        if (colon < 0)
            return false;

        int start = json.IndexOf('"', colon + 1);
        if (start < 0)
            return false;

        bool escaped = false;
        for (int i = start + 1; i < json.Length; i++)
        {
            char c = json[i];
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\')
            {
                escaped = true;
                continue;
            }

            if (c == '"')
            {
                value = UnescapeJson(json.Substring(start + 1, i - start - 1));
                return true;
            }
        }

        return false;
    }

    private static string UnescapeJson(string raw)
    {
        if (string.IsNullOrEmpty(raw) || raw.IndexOf('\\') < 0)
            return raw;

        StringBuilder sb = new StringBuilder(raw.Length);
        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];
            if (c != '\\' || i + 1 >= raw.Length)
            {
                sb.Append(c);
                continue;
            }

            char next = raw[++i];
            switch (next)
            {
                case '"': sb.Append('"'); break;
                case '\\': sb.Append('\\'); break;
                case '/': sb.Append('/'); break;
                case 'b': sb.Append('\b'); break;
                case 'f': sb.Append('\f'); break;
                case 'n': sb.Append('\n'); break;
                case 'r': sb.Append('\r'); break;
                case 't': sb.Append('\t'); break;
                case 'u':
                    if (i + 4 < raw.Length)
                    {
                        string hex = raw.Substring(i + 1, 4);
                        int code;
                        if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out code))
                        {
                            sb.Append((char)code);
                            i += 4;
                            break;
                        }
                    }
                    sb.Append('u');
                    break;
                default:
                    sb.Append(next);
                    break;
            }
        }
        return sb.ToString();
    }
}
