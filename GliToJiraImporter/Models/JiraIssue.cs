namespace GliToJiraImporter.Models
{
    public struct JiraIssue
    {
        public JiraIssueFields fields { get; set; } = new();

        public JiraIssue() { }

        public JiraIssue(string projectKey, string issueTypeName, string summary, string gliClauseId, string gliCategory, string gliSubcategory, string description)
        {
            this.fields = new JiraIssueFields(projectKey, issueTypeName, summary, gliClauseId, gliCategory, gliSubcategory, description);
        }
    }

    public struct JiraIssueFields
    {
        public JiraProject project { get; set; } = new();
        public JiraIssueType issuetype { get; set; } = new();
        public string summary { get; set; } = string.Empty;
        public string customfield_10046 { get; set; } = string.Empty; //GliClauseId
        public string customfield_10044 { get; set; } = string.Empty; //GliCategory
        public string customfield_10045 { get; set; } = string.Empty; //GliSubcategory
        public string description { get; set; } = string.Empty;

        public JiraIssueFields() { }

        public JiraIssueFields(string projectKey, string issueTypeName, string summary, string gliClauseId, string gliCategory, string gliSubcategory, string description)
        {
            this.project = new JiraProject(projectKey);
            this.issuetype = new JiraIssueType(issueTypeName);
            this.summary = summary;
            this.customfield_10046 = gliClauseId;
            this.customfield_10044 = gliCategory;
            this.customfield_10045 = gliSubcategory;
            this.description = description;
        }
    }

    public struct JiraProject
    {
        public string key { get; set; } = string.Empty;

        public JiraProject(string key)
        {
            this.key = key;
        }
    }

    public struct JiraIssueType
    {
        public string name { get; set; } = string.Empty;

        public JiraIssueType(string name)
        {
            this.name = name;
        }
    }
}
