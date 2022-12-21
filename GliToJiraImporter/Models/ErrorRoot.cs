namespace GliToJiraImporter.Models
{
    public class ErrorRoot
    {
        public List<string> ErrorMessages { get; set; } = new();
        public List<object> WarningMessages { get; set; } = new();
    }
}