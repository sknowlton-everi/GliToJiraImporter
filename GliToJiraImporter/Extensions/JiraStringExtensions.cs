using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace GliToJiraImporter.Extensions
{
    public static class JiraStringExtensions
    {
        public static bool CheckRowFormatting(this string value, int headerLength)
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

            if (headerLength != value.Substring(1, value.Length - 2).Split("|").Length)
            {
                return false;
            }

            return true;
        }
    }
}