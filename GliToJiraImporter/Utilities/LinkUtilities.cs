using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliToJiraImporter.Utilities
{
    public static class LinkUtilities
    {
        public static bool IsValidWebLink(string text)
        {
            Uri uriResult;
            return Uri.TryCreate(text.Trim(), UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        public static bool IsValidEmailAddress(string text)
        {
            return new EmailAddressAttribute().IsValid(text.Trim());
        }
    }
}
