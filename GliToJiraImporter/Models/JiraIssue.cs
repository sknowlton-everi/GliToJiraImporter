using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GliToJiraImporter.Models
{
    public struct JiraIssue
    {
        public JiraIssueFields fields { get; set; } = new JiraIssueFields();

        public JiraIssue() { }

        public JiraIssue(string projectKey, string issueTypeName, string summary, string gliClauseId, string gliCategory, string gliSubcategory, string description)
        {
            this.fields = new JiraIssueFields(projectKey, issueTypeName, summary, gliClauseId, gliCategory, gliSubcategory, description);
        }
    }

    public struct JiraIssueFields
    {
        [JsonPropertyName("project")]
        public JiraProject Project { get; set; } = new JiraProject();
        [JsonPropertyName("issuetype")]
        public JiraIssueType IssueType { get; set; } = new JiraIssueType();
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;
        [JsonPropertyName("customfield_10046")]
        public string GliClauseId { get; set; } = string.Empty;
        [JsonPropertyName("customfield_10044")]
        public string GliCategory { get; set; } = string.Empty;
        [JsonPropertyName("customfield_10045")]
        public string GliSubcategory { get; set; } = string.Empty;
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        public JiraIssueFields() { }

        public JiraIssueFields(string projectKey, string issueTypeName, string summary, string gliClauseId, string gliCategory, string gliSubcategory, string description)
        {
            this.Project = new JiraProject(projectKey);
            IssueType = new JiraIssueType(issueTypeName);
            Summary = summary;
            GliClauseId = gliClauseId;
            GliCategory = gliCategory;
            GliSubcategory = gliSubcategory;
            Description = description;
        }
    }

    public struct JiraProject
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        public JiraProject(string key)
        {
            this.Key = key;
        }
    }

    public struct JiraIssueType
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        public JiraIssueType(string name)
        {
            this.Name = name;
        }
    }
}
