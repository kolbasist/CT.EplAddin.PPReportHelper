using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Eplan.EplApi.Base;
using Eplan.EplApi.DataModel;

namespace CT.Epladdin.PPReportHelper
{
    internal static class MultiLangStringTextExtractor
    {
        private const string UndefinedLanguageCode = "??_??";

        private static readonly Regex LanguageRecordPrefixRegex =
            new Regex(
                @"(^|[;\r\n])\s*(?<language>(L_)?([A-Za-z]{2}[_-][A-Za-z]{2}|\?\?_\?\?|___|____))\s*[@:=\t]",
                RegexOptions.Compiled);

        private sealed class LanguageTextSegment
        {
            internal string LanguageCode { get; private set; }
            internal string Text { get; private set; }

            internal LanguageTextSegment(
                string languageCode,
                string text)
            {
                LanguageCode =
                    NormalizeLanguageCode(languageCode);

                Text =
                    NormalizeLanguageSegmentText(text);
            }
        }

        internal static string GetProjectLanguageText(
            MultiLangString multiLangString,
            Project project)
        {
            if (multiLangString == null)
            {
                return string.Empty;
            }

            string projectLanguageCode =
                GetProjectLanguageCode(project);

            string result;

            if (!string.IsNullOrWhiteSpace(projectLanguageCode) &&
                TryGetTextForLanguageCode(
                    multiLangString,
                    projectLanguageCode,
                    out result))
            {
                return ExtractTextFromSerializedLanguageString(
                    result,
                    projectLanguageCode);
            }

            if (TryGetTextForLanguageCode(
                    multiLangString,
                    UndefinedLanguageCode,
                    out result))
            {
                return ExtractTextFromSerializedLanguageString(
                    result,
                    UndefinedLanguageCode);
            }

            string raw =
                Normalize(multiLangString.GetAsString());

            return ExtractTextFromSerializedLanguageString(
                raw,
                projectLanguageCode);
        }

        internal static string GetProjectLanguageTextFromSerializedString(
            string value,
            Project project)
        {
            string projectLanguageCode =
                GetProjectLanguageCode(project);

            return ExtractTextFromSerializedLanguageString(
                value,
                projectLanguageCode);
        }

        internal static string NormalizeTextForPropertyGrid(
            string value)
        {
            string raw =
                Normalize(value);

            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            List<LanguageTextSegment> segments =
                ExtractLanguageTextSegments(raw);

            if (segments.Count > 0)
            {
                LanguageTextSegment undefinedSegment =
                    FindSegmentByLanguageCode(
                        segments,
                        UndefinedLanguageCode);

                if (undefinedSegment != null &&
                    !string.IsNullOrWhiteSpace(undefinedSegment.Text))
                {
                    return undefinedSegment.Text;
                }

                LanguageTextSegment firstNonEmptySegment =
                    segments.FirstOrDefault(
                        segment =>
                            segment != null &&
                            !string.IsNullOrWhiteSpace(segment.Text));

                return firstNonEmptySegment == null
                    ? string.Empty
                    : firstNonEmptySegment.Text;
            }

            return NormalizeLanguageSegmentText(
                StripFirstKnownLanguagePrefix(raw));
        }

        internal static string Normalize(
            string value)
        {
            return (value ?? string.Empty)
                .Replace("\r\n", "\n")
                .Trim();
        }

        private static string ExtractTextFromSerializedLanguageString(
            string raw,
            string projectLanguageCode)
        {
            raw =
                Normalize(raw);

            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            List<LanguageTextSegment> segments =
                ExtractLanguageTextSegments(raw);

            if (segments.Count == 0)
            {
                return NormalizeTextForPropertyGrid(raw);
            }

            LanguageTextSegment projectSegment =
                FindSegmentByLanguageCode(
                    segments,
                    projectLanguageCode);

            if (projectSegment != null &&
                !string.IsNullOrWhiteSpace(projectSegment.Text))
            {
                return projectSegment.Text;
            }

            LanguageTextSegment undefinedSegment =
                FindSegmentByLanguageCode(
                    segments,
                    UndefinedLanguageCode);

            if (undefinedSegment != null &&
                !string.IsNullOrWhiteSpace(undefinedSegment.Text))
            {
                return undefinedSegment.Text;
            }

            LanguageTextSegment firstNonEmptySegment =
                segments
                    .FirstOrDefault(
                        segment =>
                            segment != null &&
                            !string.IsNullOrWhiteSpace(segment.Text));

            return firstNonEmptySegment == null
                ? string.Empty
                : firstNonEmptySegment.Text;
        }

        private static LanguageTextSegment FindSegmentByLanguageCode(
            IEnumerable<LanguageTextSegment> segments,
            string languageCode)
        {
            if (segments == null ||
                string.IsNullOrWhiteSpace(languageCode))
            {
                return null;
            }

            string normalizedLanguageCode =
                NormalizeLanguageCode(languageCode);

            return segments.FirstOrDefault(
                segment =>
                    segment != null &&
                    string.Equals(
                        segment.LanguageCode,
                        normalizedLanguageCode,
                        StringComparison.OrdinalIgnoreCase));
        }

        private static List<LanguageTextSegment> ExtractLanguageTextSegments(
            string raw)
        {
            List<LanguageTextSegment> result =
                new List<LanguageTextSegment>();

            if (string.IsNullOrWhiteSpace(raw))
            {
                return result;
            }

            MatchCollection matches =
                LanguageRecordPrefixRegex.Matches(raw);

            if (matches == null || matches.Count == 0)
            {
                return result;
            }

            for (int index = 0; index < matches.Count; index++)
            {
                Match match =
                    matches[index];

                if (!match.Success)
                {
                    continue;
                }

                string languageCode =
                    match.Groups["language"].Value;

                int valueStartIndex =
                    match.Index + match.Length;

                int valueEndIndex =
                    index + 1 < matches.Count
                        ? matches[index + 1].Index
                        : raw.Length;

                if (valueEndIndex < valueStartIndex)
                {
                    continue;
                }

                string value =
                    raw.Substring(
                        valueStartIndex,
                        valueEndIndex - valueStartIndex);

                result.Add(
                    new LanguageTextSegment(
                        languageCode,
                        value));
            }

            return result;
        }

        private static string NormalizeLanguageSegmentText(
            string value)
        {
            string result =
                Normalize(value);

            while (result.EndsWith(";", StringComparison.Ordinal))
            {
                result =
                    Normalize(
                        result.Substring(
                            0,
                            result.Length - 1));
            }

            return result;
        }

        private static string GetProjectLanguageCode(
            Project project)
        {
            if (project == null || !project.IsValid)
            {
                return string.Empty;
            }

            string[] propertyNames =
            {
                "ProjectLanguage",
                "SourceLanguage",
                "Language",
                "DisplayLanguage",
                "TranslationLanguage",
                "MainLanguage"
            };

            foreach (string propertyName in propertyNames)
            {
                object value =
                    TryGetPublicPropertyValue(project, propertyName);

                string languageCode =
                    NormalizeLanguageCode(value);

                if (!string.IsNullOrWhiteSpace(languageCode))
                {
                    return languageCode;
                }
            }

            string[] methodNames =
            {
                "GetProjectLanguage",
                "GetSourceLanguage",
                "GetLanguage",
                "GetDisplayLanguage",
                "GetTranslationLanguage"
            };

            foreach (string methodName in methodNames)
            {
                object value =
                    TryInvokePublicMethod(project, methodName);

                string languageCode =
                    NormalizeLanguageCode(value);

                if (!string.IsNullOrWhiteSpace(languageCode))
                {
                    return languageCode;
                }
            }

            return string.Empty;
        }

        private static bool TryGetTextForLanguageCode(
            MultiLangString multiLangString,
            string languageCode,
            out string result)
        {
            result = string.Empty;

            if (multiLangString == null ||
                string.IsNullOrWhiteSpace(languageCode))
            {
                return false;
            }

            MethodInfo[] methods =
                multiLangString
                    .GetType()
                    .GetMethods()
                    .Where(method => method.Name == "GetString")
                    .ToArray();

            foreach (MethodInfo method in methods)
            {
                ParameterInfo[] parameters =
                    method.GetParameters();

                if (parameters.Length != 1)
                {
                    continue;
                }

                object argument;

                if (!TryCreateLanguageArgument(
                        parameters[0].ParameterType,
                        languageCode,
                        out argument))
                {
                    continue;
                }

                try
                {
                    object value =
                        method.Invoke(
                            multiLangString,
                            new object[] { argument });

                    result =
                        Normalize(value == null ? string.Empty : value.ToString());

                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        return true;
                    }
                }
                catch
                {
                }
            }

            return false;
        }

        private static bool TryCreateLanguageArgument(
            Type parameterType,
            string languageCode,
            out object argument)
        {
            argument = null;

            if (parameterType == typeof(string))
            {
                argument = languageCode;
                return true;
            }

            if (!parameterType.IsEnum)
            {
                return false;
            }

            string normalizedCode =
                NormalizeLanguageCode(languageCode);

            string[] candidateNames =
            {
                "L_" + normalizedCode,
                normalizedCode,
                "L_" + normalizedCode.Replace("-", "_"),
                normalizedCode.Replace("-", "_")
            };

            if (normalizedCode == UndefinedLanguageCode)
            {
                candidateNames =
                    new[]
                    {
                        "L___",
                        "L____",
                        "L_??_??",
                        "L_" + UndefinedLanguageCode,
                        UndefinedLanguageCode
                    };
            }

            foreach (string candidateName in candidateNames)
            {
                try
                {
                    if (Enum.IsDefined(parameterType, candidateName))
                    {
                        argument =
                            Enum.Parse(
                                parameterType,
                                candidateName);

                        return true;
                    }
                }
                catch
                {
                }
            }

            foreach (string enumName in Enum.GetNames(parameterType))
            {
                string enumCode =
                    NormalizeLanguageCode(enumName);

                if (string.Equals(
                        enumCode,
                        normalizedCode,
                        StringComparison.OrdinalIgnoreCase))
                {
                    argument =
                        Enum.Parse(
                            parameterType,
                            enumName);

                    return true;
                }
            }

            return false;
        }

        private static string NormalizeLanguageCode(
            object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            string text =
                value.ToString();

            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            text =
                text.Trim();

            if (text.StartsWith("L_", StringComparison.Ordinal))
            {
                text = text.Substring(2);
            }

            text =
                text.Replace('-', '_');

            if (text == "___" || text == "____")
            {
                return UndefinedLanguageCode;
            }

            return text;
        }

        private static bool TryExtractLanguageLine(
            string raw,
            string languageCode,
            out string result)
        {
            result = string.Empty;

            if (string.IsNullOrWhiteSpace(raw) ||
                string.IsNullOrWhiteSpace(languageCode))
            {
                return false;
            }

            string normalizedLanguageCode =
                NormalizeLanguageCode(languageCode);

            string[] parts =
                raw.Split(
                    new[] { '\n', ';' },
                    StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                string line =
                    part.Trim();

                if (line.Length == 0)
                {
                    continue;
                }

                if (!StartsWithLanguageCode(
                        line,
                        normalizedLanguageCode))
                {
                    continue;
                }

                result =
                    RemoveLanguagePrefix(
                        line,
                        normalizedLanguageCode);

                return !string.IsNullOrWhiteSpace(result);
            }

            return false;
        }

        private static bool StartsWithLanguageCode(
            string text,
            string languageCode)
        {
            if (string.IsNullOrWhiteSpace(text) ||
                string.IsNullOrWhiteSpace(languageCode))
            {
                return false;
            }

            string[] prefixes =
            {
                languageCode,
                "L_" + languageCode,
                "[" + languageCode + "]",
                "<" + languageCode + ">",
                languageCode + "@",
                languageCode + ":",
                languageCode + "=",
                languageCode + "\t"
            };

            return prefixes.Any(
                prefix =>
                    text.StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase));
        }

        private static string RemoveLanguagePrefix(
            string text,
            string languageCode)
        {
            string result =
                text ?? string.Empty;

            string[] prefixes =
            {
                "[" + languageCode + "]",
                "<" + languageCode + ">",
                "L_" + languageCode,
                languageCode
            };

            foreach (string prefix in prefixes)
            {
                if (result.StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    result =
                        result.Substring(prefix.Length);
                    break;
                }
            }

            result =
                result.TrimStart();

            if (result.StartsWith("@") ||
                result.StartsWith(":") ||
                result.StartsWith("="))
            {
                result =
                    result.Substring(1);
            }

            return Normalize(result);
        }

        private static string StripFirstKnownLanguagePrefix(
            string raw)
        {
            string text =
                Normalize(raw);

            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            /*
             * Важно: не режем строку по первому \n.
             * Заголовки столбцов штатно могут быть многострочными:
             * "№\nп/п", "Обозначение\nвентустановки" и т. п.
             * Здесь нужно только снять возможный языковой префикс в начале
             * всей строки, сохранив остальные переводы строк.
             */
            int separatorIndex =
                FindFirstSeparatorIndex(text);

            if (separatorIndex <= 0)
            {
                return text;
            }

            string possibleLanguage =
                text.Substring(0, separatorIndex).Trim();

            if (LooksLikeLanguageCode(possibleLanguage))
            {
                return Normalize(
                    text.Substring(separatorIndex + 1));
            }

            return text;
        }

        private static int FindFirstSeparatorIndex(
            string text)
        {
            int result = -1;

            char[] separators =
            {
                '@',
                ':',
                '=',
                '\t'
            };

            foreach (char separator in separators)
            {
                int index =
                    text.IndexOf(separator);

                if (index >= 0 &&
                    (result < 0 || index < result))
                {
                    result = index;
                }
            }

            return result;
        }

        private static bool LooksLikeLanguageCode(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            value =
                value.Trim();

            if (value.StartsWith("L_", StringComparison.Ordinal))
            {
                value = value.Substring(2);
            }

            return value == UndefinedLanguageCode ||
                   value.Length == 5 && value[2] == '_';
        }

        private static object TryGetPublicPropertyValue(
            object source,
            string propertyName)
        {
            if (source == null)
            {
                return null;
            }

            try
            {
                PropertyInfo property =
                    source.GetType().GetProperty(propertyName);

                if (property == null)
                {
                    return null;
                }

                return property.GetValue(source, null);
            }
            catch
            {
                return null;
            }
        }

        private static object TryInvokePublicMethod(
            object source,
            string methodName)
        {
            if (source == null)
            {
                return null;
            }

            try
            {
                MethodInfo method =
                    source
                        .GetType()
                        .GetMethod(
                            methodName,
                            Type.EmptyTypes);

                if (method == null)
                {
                    return null;
                }

                return method.Invoke(source, null);
            }
            catch
            {
                return null;
            }
        }
    }
}
