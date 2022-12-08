namespace GliToJiraImporter.Models
{
    public class ErrorRoot
    {
        public List<string> ErrorMessages { get; set; } = new List<string>();
        public List<object> WarningMessages { get; set; } = new List<object>();
    }
}