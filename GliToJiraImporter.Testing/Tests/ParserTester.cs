using Atlassian.Jira;
using GliToJiraImporter.Models;
using GliToJiraImporter.Utilities;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Repository;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Issue = Atlassian.Jira.Issue;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Project = Atlassian.Jira.Project;

namespace GliToJiraImporter.Testing.Tests
{
    public class ParserTester
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly string checkoffFolderName = @"Public/TestCheckoffs/";
        private static readonly string expectedResultFolderName = @"Public/ExpectedResults/";
        private const string CLAUSE_ID = "customfield_10046";
        private const string CATEGORY = "customfield_10044";
        private const string SUBCATEGORY = "customfield_10045";
        private Parser sut;
        private ParameterModel parameterModelStub;
        //private Jira jiraConnectionStub;
        //private Project jiraProjectStub;
        private IList<CategoryModel> expectedResult = new List<CategoryModel>();
        private MemoryAppender memoryAppender;

        [SetUp]
        public void Setup()
        {
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            this.memoryAppender = new log4net.Appender.MemoryAppender();
            BasicConfigurator.Configure(this.memoryAppender);

            log.Info(message: new StackFrame().GetMethod().Name);

            //parameterModelStub = new()
            //{
            //    FilePath = $"{path}Australia-New-Zealand.docx",
            //    Type = 1,
            //    SleepTime = 0,
            //    IssueType = "Task",
            //    JiraUrl = "http://jira.austin.mgam/",
            //    Password = "Password#1",
            //    ProjectKey = "STP",
            //    UserName = "JiraBot"
            //};
            //ParameterModel

            string userName = "samantha.knowlton@everi.com";
            string token = Environment.GetEnvironmentVariable("JIRA_API_TOKEN");
            string userNameToken = $"{userName}:{token}";

            parameterModelStub = new()
            {
                FilePath = $"{checkoffFolderName}Australia-New-Zealand.docx",
                Method = new HttpMethod("GET"),
                JiraUrl = "https://gre-team.atlassian.net/rest/api/2",//search?jql=project=EGRE&maxResults=10",
                UserName = userNameToken,
                //Password = string.Empty,
                IssueType = "Test Plan",
                SleepTime = 1000,
                ProjectKey = "EGRE",
                Type = 1,
            };


            //try
            //{
            //    using HttpClient httpClient = new();
            //    using HttpRequestMessage request = new(parameterModelStub.Method, parameterModelStub.JiraUrl);
            //    string base64authorization =
            //        Convert.ToBase64String(Encoding.ASCII.GetBytes(parameterModelStub.UserName));
            //    request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");

            //    HttpResponseMessage response = httpClient.Send(request);
            //    if (response.StatusCode == HttpStatusCode.OK)
            //    {
            //        string jsonString = response.Content.ReadAsStringAsync().Result;
            //        Models.Issue jsonObject = JsonConvert.DeserializeObject<Models.Issue>(jsonString);
            //        log.Debug(jsonObject.ToString());
            //    }
            //    else
            //    {
            //        string jsonString = response.Content.ReadAsStringAsync().Result;
            //        //ErrorRoot jsonObject = JsonConvert.DeserializeObject<ErrorRoot>(jsonString);
            //        log.Debug(jsonString);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    log.Error(ex);
            //}

            //TODO Samantha, see Jira URL above, change for each request, once you have the correct request, then update the Parser (sut) parameters below to
            //match what you get back from the above jira calls
            sut = new Parser(this.parameterModelStub);//, this.jiraConnectionStub);
        }

        [TearDown] //TODO This doesn't work yet
        public void TearDown()
        {
            JiraRequestUtilities jiraRequestUtilities = new JiraRequestUtilities(this.parameterModelStub);
            //int index = 0;
            //int itemsPerPage = 50;
            //string queryString = string.Format("project = {0}", parameterModelStub.ProjectKey);
            //IPagedQueryResult<Issue> jiraExistingIssueList = this.jiraConnectionStub.Issues.GetIssuesFromJqlAsync(queryString, itemsPerPage, index).Result;
            IList<Models.Issue> jiraExistingIssueList = jiraRequestUtilities.GetAllIssuesWithAClauseId();
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
                        Models.Issue issueFound = jiraExistingIssueList.First(issue => issue.fields.customfield_10046.Equals(this.expectedResult[i].RegulationList[j].ClauseID.FullClauseId));
                        if (issueFound.fields.customfield_10046 != null && issueFound.fields.customfield_10046.Equals(this.expectedResult[i].RegulationList[j].ClauseID.FullClauseId))//categories.Contains(issue["GLICategory"].Value) && issue.Labels.Count() == 0)//TODO not a good enough check
                        {
                            bool success = jiraRequestUtilities.DeleteIssueByKey(issueFound.key);//this.deleteIssueByKey(issue.Key.Value);
                            if (success != true)
                            {
                                log.Error($"Issue failed to delete. {issueFound.key}");
                            }
                        }
                        Thread.Sleep(this.parameterModelStub.SleepTime);
                    }
                    expectedResult.Clear();
                }
                checkForErrorsInLogs();
            }
        }

        //[Ignore("Can only run locally with a local Jira.")]
        [Test]
        public void ParserSingleTest()
        {
            //given
            parameterModelStub.FilePath = $"{GetFolderPath(checkoffFolderName)}SINGLE-Australia-New-Zealand.docx";
            expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{GetFolderPath(expectedResultFolderName)}ParserSingleTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            this.testAssertModel(expectedResult, result);
        }

        //[Ignore("Can only run locally with a local Jira.")]
        [Test]
        public void ParserSingleMultiDescTest()
        {
            //given
            parameterModelStub.FilePath = $"{GetFolderPath(checkoffFolderName)}SINGLE-MULTIDESC-Australia-New-Zealand.docx";
            expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{GetFolderPath(expectedResultFolderName)}ParserSingleMultiDescTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            this.testAssertModel(expectedResult, result);
        }

        //[Ignore("Can only run locally with a local Jira.")]
        [Test]
        public void ParserPicturesTest()
        {
            //given
            parameterModelStub.FilePath = $"{GetFolderPath(checkoffFolderName)}PICTURES-SHORT-Australia-New-Zealand.docx";
            expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{GetFolderPath(expectedResultFolderName)}PicturesTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.That(expectedResult, !Is.Null);
            this.testAssertModel(expectedResult, result);
        }

        //[Ignore("Does not work due to more then 50 tasks")]
        [Test]
        public void ParserSpecialsTest()
        {
            //given
            parameterModelStub.FilePath = $"{GetFolderPath(checkoffFolderName)}SPECIALS-Australia-New-Zealand.docx";
            expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{GetFolderPath(expectedResultFolderName)}ParserSpecialsTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.That(expectedResult, !Is.Null);
            this.testAssertModel(expectedResult, result);
        }

        [Ignore("This test is very large and takes a long time to complete.")]
        [Test]
        public void ParserFullTest()
        {
            //given
            //expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{GetFolderPath(expectedResultFolderName)}FullTestSearchExpectedResult.json"));
            parameterModelStub.FilePath = $"{GetFolderPath(checkoffFolderName)}Australia-New-Zealand.docx";
            int expectedCount = 633;
            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.That(result.Count, !Is.EqualTo(0));
            int totalRegs = 0;
            foreach (CategoryModel categoryModel in result)
            {
                totalRegs += categoryModel.RegulationList.Count;
            }
            Assert.That(totalRegs, Is.EqualTo(expectedCount));
            //this.testAssertModel(expectedResult, result);
        }

        //[Ignore("Can only run locally with a local Jira.")]
        [Test]
        public void ParserUnknownDocTypeTest()
        {
            //given
            parameterModelStub.FilePath = $"{GetFolderPath(checkoffFolderName)}SINGLE-Australia-New-Zealand.docx";
            parameterModelStub.Type = 0;
            IList<CategoryModel> result = new List<CategoryModel>();
            //when
            try
            {
                result = sut.Parse();
            }
            catch (Exception e)
            {
                //then
                Assert.That(e.Message, Is.EqualTo("Provided Document Type is Unknown. Try type 1 for Checkoff documents."));
                Assert.That(result, Is.Empty);
            }
        }

        //[Ignore("Can only run locally with a local Jira.")]
        [Test]
        public void ParserCharFormatTest()
        {
            //given
            parameterModelStub.FilePath = $"{GetFolderPath(checkoffFolderName)}CHARFORMAT-Australia-New-Zealand.docx";
            expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{GetFolderPath(expectedResultFolderName)}ParserCharFormatTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.That(expectedResult, !Is.Null);
            this.testAssertModel(expectedResult, result);
        }

        //[Ignore("Can only run locally with a local Jira.")]
        [Test]
        public void ParserClauseIdVarietiesTest()
        {
            //given
            parameterModelStub.FilePath = $"{GetFolderPath(checkoffFolderName)}CLAUSEID-VARIETIES.docx";
            expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{GetFolderPath(expectedResultFolderName)}ParserClauseIdVarietiesTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.That(expectedResult, !Is.Null);
            this.testAssertModel(expectedResult, result);
        }

        //[Ignore("Can only run locally with a local Jira.")]
        [Test]
        public void ParserNoCategoryTest()
        {
            //given
            parameterModelStub.FilePath = $"{GetFolderPath(checkoffFolderName)}NO-CATEGORY-Australia-New-Zealand.docx";
            expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{GetFolderPath(expectedResultFolderName)}ParserNoCategoryTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.That(expectedResult, !Is.Null);
            this.testAssertModel(expectedResult, result);
        }

        //[Ignore("Can only run locally with a local Jira.")]
        [Test]
        public void ParserLinkTest()
        {
            //given
            parameterModelStub.FilePath = $"{GetFolderPath(checkoffFolderName)}LINKs-Australia-New-Zealand.docx";
            expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{GetFolderPath(expectedResultFolderName)}ParserLinkTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.That(expectedResult, !Is.Null);
            this.testAssertModel(expectedResult, result);
        }

        //[Ignore("Can only run locally with a local Jira.")]
        [Test]
        public void ParserSingleDuplicateTest()
        {
            //given
            parameterModelStub.FilePath = $"{GetFolderPath(checkoffFolderName)}SINGLE-Australia-New-Zealand.docx";
            expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{GetFolderPath(expectedResultFolderName)}ParserSingleTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();
            result = sut.Parse();

            //then
            memoryAppender.GetEvents().First(logEvent => logEvent.Level == Level.Debug
            && logEvent.RenderedMessage.Equals($"Skipping clauseId {expectedResult[0].RegulationList[0].ClauseID.BaseClauseId} because it already exists in the project {parameterModelStub.ProjectKey}"));
            Assert.That(result.Any(), Is.True);
            Assert.That(expectedResult, !Is.Null);
            this.testAssertModel(expectedResult, result);
        }

        private void testAssertModel(IList<CategoryModel> expectedResult, IList<CategoryModel> result)
        {
            this.checkForErrorsInLogs();
            Assert.That(result, !Is.Null);
            Assert.That(result.Count, Is.EqualTo(expectedResult.Count), $"The result count of categories does not match the expected.");
            for (int i = 0; i < result.Count; i++)
            {
                Assert.That(result[i].Category, Is.EqualTo(expectedResult[i].Category), $"The Category of {result[i].Category} does not match the expected.");
                Assert.That(result[i].RegulationList.Count, Is.EqualTo(expectedResult[i].RegulationList.Count), $"The RegulationList count of \"{result[i].Category}\" does not match the expected.");

                for (int j = 0; j < result[i].RegulationList.Count; j++)
                {
                    RegulationModel resultRegulation = (RegulationModel)result[i].RegulationList[j];
                    RegulationModel expectedResultRegulation = (RegulationModel)expectedResult[i].RegulationList[j];
                    Assert.That(resultRegulation.ClauseID.BaseClauseId, Is.EqualTo(expectedResultRegulation.ClauseID.BaseClauseId), $"The BaseClauseId of {resultRegulation.ClauseID.BaseClauseId} does not match the expected.");
                    Assert.That(resultRegulation.ClauseID.FullClauseId, Is.EqualTo(expectedResultRegulation.ClauseID.FullClauseId), $"The FullClauseId of {resultRegulation.ClauseID.FullClauseId} does not match the expected.");
                    Assert.That(resultRegulation.Subcategory, Is.EqualTo(expectedResultRegulation.Subcategory), $"The Subcategory of {resultRegulation.ClauseID.FullClauseId} does not match the expected.");
                    Assert.That(resultRegulation.Description.Text, Is.EqualTo(expectedResultRegulation.Description.Text), $"The Description Text of {resultRegulation.ClauseID.FullClauseId} does not match the expected.");

                    IList<PictureModel> resultAttachmentList = ((DescriptionModel)resultRegulation.Description).AttachmentList;
                    IList<PictureModel> expectedResultAttachmentList = ((DescriptionModel)expectedResultRegulation.Description).AttachmentList;
                    Assert.That(resultAttachmentList.Count, Is.EqualTo(expectedResultAttachmentList.Count), $"The AttachmentList of {resultRegulation.ClauseID.FullClauseId} does not match the expected.");
                    for (int k = 0; k < resultAttachmentList.Count; k++)
                    {
                        Assert.That(resultAttachmentList[k].ImageName, Is.EqualTo(expectedResultAttachmentList[k].ImageName), $"ImageName at position {k} of {resultRegulation.ClauseID.FullClauseId} does not match the expected.");
                        Assert.That(resultAttachmentList[k].ImageBytes, Is.EqualTo(expectedResultAttachmentList[k].ImageBytes), $"ImageBytes at position {k} of {resultRegulation.ClauseID.FullClauseId} does not match the expected.");
                    }
                }
            }
        }

        private void checkForErrorsInLogs()
        {
            LoggingEvent[] logEvents = memoryAppender.GetEvents();
            foreach (LoggingEvent logEvent in logEvents)
            {
                Assert.That(logEvent.Level == Level.Info || logEvent.Level == Level.Debug, $"There was an error in the logs. \"{logEvent.RenderedMessage}\"");
            }
        }

        //private bool deleteIssueByKey(string issueKey)
        //{
        //    bool success = false;

        //    Task t = this.jiraConnectionStub.Issues.DeleteIssueAsync(issueKey);

        //    while (t.Status == TaskStatus.Running || t.Status == TaskStatus.WaitingForChildrenToComplete || t.Status == TaskStatus.WaitingToRun)
        //    {
        //        log.Debug($"Waiting on issue {issueKey} to finish running. Status: {t.Status}");
        //    }

        //    success = (t.Status == TaskStatus.RanToCompletion);
        //    log.Debug($"Task complete. Status: {t.Status}");

        //    return success;
        //}

        private string GetFolderPath(string folderName)
        {
            var currentAssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Replace(@"file:/", string.Empty);
            currentAssemblyPath = currentAssemblyPath.Replace(@"bin/Debug/net6.0", string.Empty);
            var relativePath = Path.Combine(currentAssemblyPath, folderName);
            log.Debug("PATHS");
            log.Debug(currentAssemblyPath);
            log.Debug(relativePath);
            log.Debug(Path.GetFullPath(relativePath));
            //return Path.GetFullPath(relativePath);
            return relativePath;
        }
    }
}