using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Eplan.EplApi.Base;
using Eplan.EplApi.DataModel;

namespace CT.Epladdin.PPReportHelper
{
    internal static class MultiLangStringTextExtractor
    {
        private const string UndefinedLanguageCode = "??_??";

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
                return Normalize(result);
            }

            if (TryGetTextForLanguageCode(
                    multiLangString,
                    UndefinedLanguageCode,
                    out result))
            {
                return Normalize(result);
            }

            string raw =
                Normalize(multiLangString.GetAsString());

            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(projectLanguageCode) &&
                TryExtractLanguageLine(
                    raw,
                    projectLanguageCode,
                    out result))
            {
                return Normalize(result);
            }

            if (TryExtractLanguageLine(
                    raw,
                    UndefinedLanguageCode,
                    out result))
            {
                return Normalize(result);
            }

            return StripFirstKnownLanguagePrefix(raw);
        }

        internal static string Normalize(
            string value)
        {
            return (value ?? string.Empty)
                .Replace("\r\n", "\n")
                .Trim();
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

            string firstLine =
                text.Split('\n').FirstOrDefault() ?? text;

            int separatorIndex =
                FindFirstSeparatorIndex(firstLine);

            if (separatorIndex <= 0)
            {
                return firstLine;
            }

            string possibleLanguage =
                firstLine.Substring(0, separatorIndex).Trim();

            if (LooksLikeLanguageCode(possibleLanguage))
            {
                return Normalize(firstLine.Substring(separatorIndex + 1));
            }

            return firstLine;
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