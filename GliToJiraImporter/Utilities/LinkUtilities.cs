using System.ComponentModel.DataAnnotations;

namespace GliToJiraImporter.Utilities
{
    public static class LinkUtilities
    {
        public static bool IsValidWebLink(string text)
        {
            return Uri.TryCreate(text.Trim(), UriKind.Absolute, out Uri? uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        public static bool IsValidEmailAddress(string text)
        {
            return new EmailAddressAttribute().IsValid(text.Trim());
        }
    }
}
