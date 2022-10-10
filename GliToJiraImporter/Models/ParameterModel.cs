using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using System.Text.Json;

namespace GliToJiraImporter.Models
{
    public class ParameterModel
    {
        //******************************************************************************
        // REQUIRED FIELDS
        //******************************************************************************

        [Option('f', "filepath", Required = true, HelpText = "Path to GLI document to be parsed")]
        public string FilePath { get; set; }

        [Option('k', "key", Required = true, HelpText = "Jira project key that will store these requirements")]
        public string ProjectKey { get; set; }

        [Option('u', "username", Required = true, HelpText = "Jira login username")]
        public string UserName { get; set; }

        [Option('p', "password", Required = true, HelpText = "Password for the Jira user")]
        public string Password { get; set; }

        [Option('j', "jiraurl", Required = true, HelpText = "URL of the Jira instance")]
        public string JiraUrl { get; set; }

        [Option('i', "issuetype", Required = true, HelpText = "Type of Jira issue to create")]
        public string IssueType { get; set; }

        [Option('s', "sleeptime", Required = true, HelpText = "Duration in milliseconds to pause between each issue creation")]
        public int SleepTime { get; set; }

        [Option('t', "type", Required = true, HelpText = "Type of document being uploaded")]
        public int Type { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }

    }
}
