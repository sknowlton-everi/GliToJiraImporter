using GliToJiraImporter.Models;
using GliToJiraImporter.Utilities;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Repository;
using RestSharp;
using System.Diagnostics;
using System.Reflection;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace GliToJiraImporter.Testing.Tests
{
    internal class JiraRequestUtilitiesTester
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        private JiraRequestUtilities sut = new();
        private ParameterModel parameterModelStub = new();
        private MemoryAppender memoryAppender = new log4net.Appender.MemoryAppender();
        private static readonly string expectedResultFolderName = @"../../../Public/ExpectedResults/";

        [SetUp]
        public void Setup()
        {
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
            BasicConfigurator.Configure(this.memoryAppender);

            log.Info(message: new StackFrame().GetMethod().Name);

            string userName = "samantha.knowlton@everi.com";
            string token = Environment.GetEnvironmentVariable("JIRA_API_TOKEN");
            string userNameToken = $"{userName}:{token}";

            parameterModelStub = new()
            {
                FilePath = @"../../../Public/TestCheckoffs/SINGLE-Australia-New-Zealand.docx",
                Method = Method.GET,
                JiraUrl = "https://gre-team.atlassian.net",
                UserName = userNameToken,
                IssueType = "Test Plan",
                SleepTime = 1000,
                ProjectKey = "EGRE",
                Type = 1,
            };

            this.sut = new JiraRequestUtilities(this.parameterModelStub);
        }

        [TearDown] //TODO This doesn't work perfectly
        public void TearDown()
        {
            log.Debug("Teardown start"); JiraRequestUtilities jiraRequestUtilities = new JiraRequestUtilities(this.parameterModelStub); //int index = 0; //int itemsPerPage = 50; //string queryString = string.Format("project = \{0}
            ", parameterModelStub.ProjectKey);
        //IPagedQueryResult<Issue> jiraExistingIssueList = this.jiraConnectionStub.Issues.GetIssuesFromJqlAsync(queryString, itemsPerPage, index).Result;
IList<Models.Issue> jiraExistingIssueList = jiraRequestUtilities.GetAllIssuesWithAClauseId();
            if (jiraExistingIssueList == null)

            { log.Error("JiraRequestUtilities.GetAllIssuesWithAClauseId returned null, and therefore failed"); return; }
            //IList<string> clauseIds = (IList<string>)expectedResult.Select(category => category.RegulationList.Select(regulation => regulation.ClauseID).ToList()).ToList();
            //IList<string> categories = expectedResult.Select(cat => cat.Category).ToList();
            //Dictionary<string, Issue> issues = (Dictionary<string, Issue>)this.jiraConnectionStub.Issues.GetIssuesAsync().Result;
            //foreach (Models.Issue issue in jiraExistingIssueList)
            if (jiraExistingIssueList.Count > 0)
            {
                for (int i = 0; i < this.expectedResult.Count; i++)
                {
                    for (int j = 0; j < this.expectedResult[i].RegulationList.Count; j++)
                    {
                        //Models.Issue issue = jiraExistingIssueList[i];
                        Models.Issue issueFound = jiraRequestUtilities.GetIssueByClauseId(this.expectedResult[i].RegulationList[j].ClauseId.FullClauseId);
                        //Models.Issue issueFound = jiraExistingIssueList.First(issue => issue.fields.customfield_10046.Equals(this.expectedResult[i].RegulationList[j].ClauseId.FullClauseId));
                        if (issueFound != null && issueFound.fields.customfield_10046.Equals(this.expectedResult[i].RegulationList[j].ClauseId.FullClauseId))//categories.Contains(issue["GLICategory"].Value) && issue.Labels.Count() == 0)//TODO not a good enough check
                        {
                            bool success = jiraRequestUtilities.DeleteIssueByKey(issueFound.key);//this.deleteIssueByKey(issue.Key.Value);
                            if (success != true)
                            {
                                log.Error($"Issue failed to delete. \{issueFound.key}
                            ");
                            }
                        }
                        Thread.Sleep(this.parameterModelStub.SleepTime);
                    }
                    expectedResult.Clear();
                }
                checkForErrorsInLogs();
            }
            log.Debug("Teardown end");
        }

        [Test]
        public void ParserSingleDuplicateTest()
        {
            //given
            //parameterModelStub.FilePath = $"{checkoffFolderName}SINGLE-Australia-New-Zealand.docx";
            IList<CategoryModel> categorys = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{expectedResultFolderName}ParserSingleTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.
            result = sut.Parse();

            //then
            memoryAppender.GetEvents().First(logEvent => logEvent.Level == Level.Debug
                                                         && logEvent.RenderedMessage.Equals($"Skipping clauseId {expectedResult[0].RegulationList[0].ClauseId.BaseClauseId} because it already exists in the project {parameterModelStub.ProjectKey}"));
            Assert.That(result.Any(), Is.True);
            Assert.That(expectedResult, !Is.Null);
            this.testAssertModel(expectedResult, result);
        }
    }
}
