using System;
using System.IO;
using System.Linq;

namespace ZModLauncher;

public static class StringHelper
{
    public static string GetFileHash(string filePath)
    {
        var hasher = new DropboxContentHasher();
        var buf = new byte[1024];
        using (FileStream file = File.OpenRead(filePath))
        {
            while (true)
            {
                int n = file.Read(buf, 0, buf.Length);
                if (n <= 0) break;
                hasher.TransformBlock(buf, 0, n, buf, 0);
            }
        }
        hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        string hexHash = DropboxContentHasher.ToHex(hasher.Hash);
        return hexHash;
    }

    public static string ExtractString(string baseString, string startString, string endString)
    {
        int startStringIndex = baseString.IndexOf(startString, StringComparison.Ordinal) + startString.Length;
        int endStringIndex = baseString.IndexOf(endString, StringComparison.Ordinal);
        return baseString.Substring(startStringIndex, endStringIndex - startStringIndex);
    }

    public static string[] ExtractPathTokens(string filePath, char delimiter = ' ')
    {
        return delimiter == ' ' ? filePath.Split('/', '\\') : filePath.Split(delimiter);
    }

    public static string[] AssertExtractPathTokens(string filePath, int wantedNumTokens, char delimiter = ' ')
    {
        string[] pathTokens = ExtractPathTokens(filePath, delimiter);
        return pathTokens.Length != wantedNumTokens ? null : pathTokens;
    }

    public static bool IsFileAnImage(string fileName)
    {
        return Enum.GetNames(typeof(ImageFileExtensions)).Any(i =>
            fileName.ToLower().EndsWith($".{i.ToLower()}"));
    }

    public static bool IsMatching(string baseString, string targetString)
    {
        return string.Equals(baseString, targetString, StringComparison.CurrentCultureIgnoreCase);
    }
}