namespace GliToJiraImporter.Models
{
    public class ErrorRoot
    {
        public IList<string> ErrorMessages { get; set; } = new List<string>();
        public IList<object> WarningMessages { get; set; } = new List<object>();
    }
}