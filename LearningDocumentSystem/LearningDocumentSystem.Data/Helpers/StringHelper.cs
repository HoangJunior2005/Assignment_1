using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LearningDocumentSystem.Common.Helpers
{
    public static class StringHelper
    {
        public static string NormalizeStudentCode(string studentCode)
            => studentCode.Trim().ToUpperInvariant();

        public static string NormalizeFullName(string fullName)
        {
            var collapsed = Regex.Replace(fullName.Trim(), @"\s+", " ");
            return RemoveDiacritics(collapsed).ToLowerInvariant();
        }

        public static bool NamesMatch(string inputName, string registryName)
            => NormalizeFullName(inputName) == NormalizeFullName(registryName);

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    builder.Append(c);
            }
            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
