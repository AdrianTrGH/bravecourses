using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Lesson_1.Lesson_01_01_grounding.Utils;

internal static class HashUtils
{
    internal static string HashText(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    internal static string HashObject(object obj)
    {
        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = false });
        return HashText(json);
    }
}
