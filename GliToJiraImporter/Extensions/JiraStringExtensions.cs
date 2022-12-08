namespace GliToJiraImporter.Extensions
{
    public static class JiraStringExtensions
    {
        public static bool IsValidRowFormatting(this string value, int headerLength)
        {
            string trimmedValue = value.Trim();
            if (trimmedValue.Equals(string.Empty))
            {
                return false;
            }

            if (!trimmedValue.StartsWith("|") && !trimmedValue.EndsWith("|"))
            {
                return false;
            }

            if (headerLength != trimmedValue.Trim('|').Split("|").Length)
            {
                return false;
            }

            return true;
        }

        public static bool IsValidHeaderRowFormatting(this string value)
        {
            string trimmedValue = value.Trim();
            if (trimmedValue.Equals(string.Empty))
            {
                return false;
            }

            if (!trimmedValue.StartsWith("||") && !trimmedValue.EndsWith("||"))
            {
                return false;
            }

            if (!(trimmedValue.Length >= 2))
            {
                return false;
            }

            return true;
        }
    }
}