using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Sensei.Components;

public static class AnswerEvaluator
{
    private static readonly Regex LetterIdentifierRegex = new(
        @"^(?:option\s+)?[\(\[\{]?([A-Za-z])[\)\]\}]?[\.\:\-]?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static readonly Regex NumberIdentifierRegex = new(
        @"^(?:option\s+)?[\(\[\{]?([1-9][0-9]*)[\)\]\}]?[\.\:\-]?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static readonly Regex LeadingPrefixRegex = new(
        @"^(?:option\s+)?[\(\[\{]?[A-Za-z0-9][\)\]\}\.\:\-]+\s*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static readonly Regex WhitespaceRegex = new(
        @"\s+",
        RegexOptions.Compiled
    );

    /// <summary>
    /// Normalizes whitespace and trims the input string.
    /// </summary>
    public static string Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        return WhitespaceRegex.Replace(s.Trim(), " ");
    }

    /// <summary>
    /// Strips leading option prefixes such as "A) ", "(A) ", "Option A: ", "1. ", "1 - ".
    /// </summary>
    public static string StripOptionPrefix(string? s)
    {
        string norm = Normalize(s);
        if (string.IsNullOrEmpty(norm)) return string.Empty;

        string stripped = LeadingPrefixRegex.Replace(norm, "").Trim();
        return stripped;
    }

    /// <summary>
    /// Safely parses options from a JSON array string.
    /// </summary>
    public static List<string> ParseOptions(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(optionsJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Checks if a string represents a standalone option letter/number identifier (e.g., "A", "(A)", "[A]", "Option A", "A.", "A)", "1", "(1)").
    /// </summary>
    public static bool TryExtractOptionIndex(string? s, int maxOptions, out int index)
    {
        index = -1;
        string norm = Normalize(s);
        if (string.IsNullOrEmpty(norm)) return false;

        var letterMatch = LetterIdentifierRegex.Match(norm);
        if (letterMatch.Success)
        {
            char letter = char.ToUpperInvariant(letterMatch.Groups[1].Value[0]);
            int idx = letter - 'A';
            if (idx >= 0 && (maxOptions <= 0 || idx < maxOptions))
            {
                index = idx;
                return true;
            }
        }

        var numberMatch = NumberIdentifierRegex.Match(norm);
        if (numberMatch.Success && int.TryParse(numberMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int num))
        {
            int idx = num - 1;
            if (idx >= 0 && (maxOptions <= 0 || idx < maxOptions))
            {
                index = idx;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Resolves an option text or identifier to its 0-based index in the given options list.
    /// Returns -1 if not found.
    /// </summary>
    public static int ResolveOptionIndex(string? text, List<string>? options)
    {
        if (options == null || options.Count == 0) return -1;
        string norm = Normalize(text);
        if (string.IsNullOrEmpty(norm)) return -1;

        // 1. Direct letter or number index (e.g., "A", "(B)", "Option C")
        if (TryExtractOptionIndex(norm, options.Count, out int directIdx))
        {
            return directIdx;
        }

        string stripped = StripOptionPrefix(norm);

        // 2. Scan options array for exact, stripped, or leading letter matches
        for (int i = 0; i < options.Count; i++)
        {
            string optNorm = Normalize(options[i]);
            if (string.Equals(optNorm, norm, StringComparison.OrdinalIgnoreCase))
                return i;

            string optStripped = StripOptionPrefix(optNorm);
            if (!string.IsNullOrEmpty(optStripped) && !string.IsNullOrEmpty(stripped) &&
                string.Equals(optStripped, stripped, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }

            if (!string.IsNullOrEmpty(optStripped) &&
                string.Equals(optStripped, norm, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }

            if (!string.IsNullOrEmpty(stripped) &&
                string.Equals(optNorm, stripped, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Resilient, comprehensive answer comparison.
    /// Matches:
    /// - Exact match (case-insensitive, whitespace normalized)
    /// - Standalone letter/number identifier ("A", "B", "C", "D", "(A)", "[A]", "A.", "Option A", "A)")
    /// - Single letter answer matching against options by index
    /// - Stripped option text matching stripped answer (removing leading option numbers/letters)
    /// - Numerical equivalence for NAT questions (e.g. 4 vs 4.0)
    /// </summary>
    public static bool IsMatch(string? expectedAnswer, string? userAnswer, List<string>? options = null)
    {
        string exp = Normalize(expectedAnswer);
        string act = Normalize(userAnswer);

        // Empty answers are never correct
        if (string.IsNullOrEmpty(exp) || string.IsNullOrEmpty(act))
            return false;

        // 1. Direct match (case-insensitive, whitespace normalized)
        if (string.Equals(exp, act, StringComparison.OrdinalIgnoreCase))
            return true;

        // 2. Both are option letters/identifiers pointing to the same index (e.g., "(A)" and "Option A")
        bool expIsId = TryExtractOptionIndex(exp, 26, out int expId);
        bool actIsId = TryExtractOptionIndex(act, 26, out int actId);
        if (expIsId && actIsId && expId == actId)
            return true;

        // 3. Stripped option prefixes match (e.g., "A) Round Robin" and "Round Robin", or "Option A: RR" and "(A) RR")
        string expStripped = StripOptionPrefix(exp);
        string actStripped = StripOptionPrefix(act);
        if (!string.IsNullOrEmpty(expStripped) && !string.IsNullOrEmpty(actStripped))
        {
            if (string.Equals(expStripped, actStripped, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // 4. Options-based resolution (letter vs text or full option comparison)
        if (options != null && options.Count > 0)
        {
            int expIdx = ResolveOptionIndex(exp, options);
            int actIdx = ResolveOptionIndex(act, options);

            // Both resolved to the same option index
            if (expIdx >= 0 && actIdx >= 0 && expIdx == actIdx)
                return true;

            // Expected is letter index, user selected text of that option
            if (expIdx >= 0 && expIdx < options.Count)
            {
                string targetOpt = options[expIdx];
                if (string.Equals(Normalize(targetOpt), act, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (!string.IsNullOrEmpty(actStripped) &&
                    string.Equals(StripOptionPrefix(targetOpt), actStripped, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            // User answered with letter index, expected is text of that option
            if (actIdx >= 0 && actIdx < options.Count)
            {
                string targetOpt = options[actIdx];
                if (string.Equals(Normalize(targetOpt), exp, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (!string.IsNullOrEmpty(expStripped) &&
                    string.Equals(StripOptionPrefix(targetOpt), expStripped, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        // 5. Letter match against leading letter of the other when options not available
        if (expIsId)
        {
            char letter = (char)('A' + expId);
            var prefixRegex = new Regex($@"^(?:option\s+)?[\(\[]?{letter}[\)\]\.\:\-]\s*", RegexOptions.IgnoreCase);
            if (prefixRegex.IsMatch(act))
                return true;
        }
        if (actIsId)
        {
            char letter = (char)('A' + actId);
            var prefixRegex = new Regex($@"^(?:option\s+)?[\(\[]?{letter}[\)\]\.\:\-]\s*", RegexOptions.IgnoreCase);
            if (prefixRegex.IsMatch(exp))
                return true;
        }

        // 6. Numerical equivalence for NAT (Numerical Answer Type) questions (e.g., 4 vs 4.0)
        if (double.TryParse(exp, NumberStyles.Float, CultureInfo.InvariantCulture, out double expVal) &&
            double.TryParse(act, NumberStyles.Float, CultureInfo.InvariantCulture, out double actVal))
        {
            if (Math.Abs(expVal - actVal) < 1e-6)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Resilient answer comparison using options JSON string.
    /// </summary>
    public static bool IsMatch(string? expectedAnswer, string? userAnswer, string? optionsJson)
    {
        var options = ParseOptions(optionsJson);
        return IsMatch(expectedAnswer, userAnswer, options);
    }
}
