using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace JNPF.Analyzers;

/// <summary>
/// Loads complexity-baseline.json (AdditionalFiles or embedded). Hand-parsed — no extra analyzer deps.
/// </summary>
internal sealed class ComplexityBaseline
{
    private static readonly Regex EntryRegex = new(
        @"\{[^{}]*?""(?:file|symbol|name|maxComplexity)""[^{}]*?\}",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex FileRegex = new(@"""file""\s*:\s*""([^""]+)""", RegexOptions.Compiled);
    private static readonly Regex NameRegex = new(@"""name""\s*:\s*""([^""]+)""", RegexOptions.Compiled);
    private static readonly Regex SymbolRegex = new(@"""symbol""\s*:\s*""([^""]+)""", RegexOptions.Compiled);
    private static readonly Regex MaxRegex = new(@"""maxComplexity""\s*:\s*(\d+)", RegexOptions.Compiled);
    private static readonly Regex ThresholdRegex = new(@"""threshold""\s*:\s*(\d+)", RegexOptions.Compiled);

    private readonly Dictionary<string, int> _byFileAndName;
    private readonly Dictionary<string, int> _bySymbol;

    public int Threshold { get; }

    private ComplexityBaseline(int threshold, Dictionary<string, int> byFileAndName, Dictionary<string, int> bySymbol)
    {
        Threshold = threshold;
        _byFileAndName = byFileAndName;
        _bySymbol = bySymbol;
    }

    public static ComplexityBaseline Load(ImmutableArray<AdditionalText> additionalFiles)
    {
        foreach (var file in additionalFiles)
        {
            if (file.Path == null)
                continue;
            var name = Path.GetFileName(file.Path);
            if (!string.Equals(name, "complexity-baseline.json", StringComparison.OrdinalIgnoreCase))
                continue;

            var text = file.GetText()?.ToString();
            if (!string.IsNullOrWhiteSpace(text))
                return Parse(text);
        }

        return Empty(threshold: 30);
    }

    public static ComplexityBaseline Parse(string json)
    {
        var threshold = 30;
        var th = ThresholdRegex.Match(json);
        if (th.Success && int.TryParse(th.Groups[1].Value, out var t))
            threshold = t;

        var byFile = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var bySymbol = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (Match m in EntryRegex.Matches(json))
        {
            var block = m.Value;
            var maxMatch = MaxRegex.Match(block);
            if (!maxMatch.Success || !int.TryParse(maxMatch.Groups[1].Value, out var maxCc))
                continue;

            var fileMatch = FileRegex.Match(block);
            var nameMatch = NameRegex.Match(block);
            if (fileMatch.Success && nameMatch.Success)
            {
                var key = NormalizeFile(fileMatch.Groups[1].Value) + "::" + nameMatch.Groups[1].Value;
                byFile[key] = maxCc;
            }

            var symbolMatch = SymbolRegex.Match(block);
            if (symbolMatch.Success)
                bySymbol[symbolMatch.Groups[1].Value] = maxCc;
        }

        return new ComplexityBaseline(threshold, byFile, bySymbol);
    }

    public static ComplexityBaseline Empty(int threshold) =>
        new ComplexityBaseline(threshold, new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));

    public bool TryGetMaxComplexity(string filePath, string methodName, out int maxComplexity)
    {
        maxComplexity = 0;
        if (string.IsNullOrEmpty(methodName))
            return false;

        var normalized = NormalizeFile(filePath);
        var key = normalized + "::" + methodName;
        if (_byFileAndName.TryGetValue(key, out maxComplexity))
            return true;

        // Suffix match: inventory paths are repo-relative under backend/
        foreach (var pair in _byFileAndName)
        {
            var sep = pair.Key.LastIndexOf("::", StringComparison.Ordinal);
            if (sep < 0)
                continue;
            var filePart = pair.Key.Substring(0, sep);
            var namePart = pair.Key.Substring(sep + 2);
            if (!string.Equals(namePart, methodName, StringComparison.Ordinal))
                continue;
            if (normalized.EndsWith(filePart, StringComparison.OrdinalIgnoreCase)
                || filePart.EndsWith(normalized, StringComparison.OrdinalIgnoreCase))
            {
                maxComplexity = pair.Value;
                return true;
            }
        }

        return false;
    }

    internal static string NormalizeFile(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;
        return path.Replace('\\', '/').TrimStart('/');
    }
}
