using Atlassian.Jira;
using GliToJiraImporter.Models;
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

        private static readonly string checkoffPath = @"..\..\..\Public\TestCheckoffs\";
        private static readonly string expectedResultPath = @"..\..\..\Public\ExpectedResults\";
        private Parser sut;
        private ParameterModel parameterModelStub;
        private Jira jiraConnectionStub;
        private Project jiraProjectStub;
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

            //TODO Samantha, change user email below and also the verify the API token
            string userName = "richard.henry@everi.com";
            string token = "fCCOc3rtz6qxzXdC0p9h30E0";
            string userNameToken = $"{userName}:{token}";

            parameterModelStub = new()
            {
                FilePath = $"{checkoffPath}Australia-New-Zealand.docx",
                Method = new HttpMethod("GET"),
                JiraUrl = "https://gre-team.atlassian.net/rest/api/3/search?jql=project=EGRE&maxResults=10",
                UserName = userNameToken,
                Password = String.Empty,
                IssueType = "Test Plan",
                SleepTime = 0,
                ProjectKey = "EGRE",
                Type = 1,
            };

            try
            {
                using HttpClient httpClient = new();
                using HttpRequestMessage request = new(parameterModelStub.Method, parameterModelStub.JiraUrl);
                string base64authorization =
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(parameterModelStub.UserName));
                request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");

                HttpResponseMessage response = httpClient.Send(request);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string jsonString = response.Content.ReadAsStringAsync().Result;
                    GliToJiraImporter.Models.Issue jsonObject = JsonConvert.DeserializeObject<GliToJiraImporter.Models.Issue>(jsonString);
                    log.Debug(jsonObject.ToString());
                }
                else
                {
                    string jsonString = response.Content.ReadAsStringAsync().Result;
                    ErrorRoot jsonObject = JsonConvert.DeserializeObject<ErrorRoot>(jsonString);
                    log.Debug(jsonObject.errorMessages);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

            //RH not longer needed this.jiraConnectionStub = Jira.CreateRestClient(parameterModelStub.JiraUrl, parameterModelStub.UserName, parameterModelStub.Password);
            //RH not longer needed this.jiraProjectStub = jiraConnectionStub.Projects.GetProjectAsync(parameterModelStub.ProjectKey).Result;

            //TODO Samantha, see Jira URL above, change for each request, once you have the correct request, then update the Parser (sut) parameters below to
            //match what you get back from the above jira calls
            //sut = new Parser(this.parameterModelStub, this.jiraConnectionStub);
        }

        //[TearDown] //TODO This doesn't work yet
        public void TearDown()
        {
            int index = 0;
            int itemsPerPage = 50;
            string queryString = string.Format("project = {0}", parameterModelStub.ProjectKey);
            IPagedQueryResult<Issue> jiraExistingIssueList = this.jiraConnectionStub.Issues.GetIssuesFromJqlAsync(queryString, itemsPerPage, index).Result;
            //IList<string> ClauseIds = expectedResult.Select(cat => cat.RegulationList.Select(reg => reg.ClauseID).ToList).ToList;
            IList<string> categories = expectedResult.Select(cat => cat.Category).ToList();
            //Dictionary<string, Issue> issues = (Dictionary<string, Issue>)this.jiraConnectionStub.Issues.GetIssuesAsync().Result;
            foreach (Issue issue in jiraExistingIssueList)
            {
                if (issue["GLICategory"] != null && categories.Contains(issue["GLICategory"].Value) && issue.Labels.Count() == 0)//TODO not a good enough check
                {
                    bool success = this.deleteIssueByKey(issue.Key.Value);
                    if (success != true)
                    {
                        log.Error($"Issue failed to delete. {issue.Key.Value}");
                    }
                }
            }
            expectedResult.Clear();
        }

        [Ignore("Can only run locally with a local Jira.")]
        [Test]
        public void ParserSingleTest()
        {
            //given
            parameterModelStub.FilePath = $"{checkoffPath}SINGLE-Australia-New-Zealand.docx";
            expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{expectedResultPath}ParserSingleTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            this.testAssertModel(expectedResult, result);
        }

        [Ignore("Can only run locally with a local Jira.")]
        [Test]
        public void ParserSingleMultiDescTest()
        {
            //given
            parameterModelStub.FilePath = $"{checkoffPath}SINGLE-MULTIDESC-Australia-New-Zealand.docx";
            expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{expectedResultPath}ParserSingleMultiDescTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            this.testAssertModel(expectedResult, result);
        }

        [Ignore("Can only run locally with a local Jira.")]
        [Test]
        public void ParserPicturesTest()
        {
            //given
            parameterModelStub.FilePath = $"{checkoffPath}PICTURES-SHORT-Australia-New-Zealand.docx";
            expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{expectedResultPath}PicturesTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.NotNull(expectedResult);
            this.testAssertModel(expectedResult, result);
        }

        [Ignore("Can only run locally with a local Jira.")]
        [Test]
        public void ParserSpecialsTest()
        {
            //given
            parameterModelStub.FilePath = $"{checkoffPath}SPECIALS-Australia-New-Zealand.docx";
            expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{expectedResultPath}ParserSpecialsTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.NotNull(expectedResult);
            this.testAssertModel(expectedResult, result);
        }

        [Test]
        public void ParserFullTest()
        {
            //given
            //expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{expectedResultPath}FullTestSearchExpectedResult.json"));
            parameterModelStub.FilePath = $"{checkoffPath}Australia-New-Zealand.docx";
            int expectedCount = 633;
            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.NotZero(result.Count);
            int totalRegs = 0;
            foreach (CategoryModel categoryModel in result)
            {
                totalRegs += categoryModel.RegulationList.Count;
            }
            Assert.That(totalRegs, Is.EqualTo(expectedCount));
            //this.testAssertModel(expectedResult, result);
        }

        [Ignore("Can only run locally with a local Jira.")]
        [Test]
        public void ParserUnknownDocTypeTest()
        {
            //given
            parameterModelStub.FilePath = $"{checkoffPath}SINGLE-Australia-New-Zealand.docx";
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

        [Ignore("Can only run locally with a local Jira.")]
        [Test]
        public void ParserCharFormatTest()
        {
            //given
            parameterModelStub.FilePath = $"{checkoffPath}CHARFORMAT-Australia-New-Zealand.docx";
            expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{expectedResultPath}ParserCharFormatTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.NotNull(expectedResult);
            this.testAssertModel(expectedResult, result);
        }

        [Ignore("Can only run locally with a local Jira.")]
        [Test]
        public void ParserClauseIdVarietiesTest()
        {
            //given
            parameterModelStub.FilePath = $"{checkoffPath}CLAUSEID-VARIETIES.docx";
            expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{expectedResultPath}ParserClauseIdVarietiesTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.NotNull(expectedResult);
            this.testAssertModel(expectedResult, result);
        }

        [Ignore("Can only run locally with a local Jira.")]
        [Test]
        public void ParserNoCategoryTest()
        {
            //given
            parameterModelStub.FilePath = $"{checkoffPath}NO-CATEGORY-Australia-New-Zealand.docx";
            expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{expectedResultPath}ParserNoCategoryTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.NotNull(expectedResult);
            this.testAssertModel(expectedResult, result);
        }

        [Ignore("Can only run locally with a local Jira.")]
        [Test]
        public void ParserLinkTest()
        {
            //given
            parameterModelStub.FilePath = $"{checkoffPath}LINKs-Australia-New-Zealand.docx";
            expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{expectedResultPath}ParserLinkTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.NotNull(expectedResult);
            this.testAssertModel(expectedResult, result);
        }

        private void testAssertModel(IList<CategoryModel> expectedResult, IList<CategoryModel> result)
        {
            this.checkForErrorsInLogs();
            Assert.NotNull(result);
            Assert.That(result.Count, Is.EqualTo(expectedResult.Count), $"The result count of categories does not match the expected.");
            for (int i = 0; i < result.Count; i++)
            {
                Assert.That(result[i].Category, Is.EqualTo(expectedResult[i].Category), $"The Category of {result[i].Category} does not match the expected.");
                Assert.That(result[i].RegulationList.Count, Is.EqualTo(expectedResult[i].RegulationList.Count), $"The RegulationList count of \"{result[i].Category}\" does not match the expected.");

                for (int j = 0; j < result[i].RegulationList.Count; j++)
                {
                    RegulationModel resultRegulation = (RegulationModel)result[i].RegulationList[j];
                    RegulationModel expectedResultRegulation = (RegulationModel)expectedResult[i].RegulationList[j];
                    Assert.That(resultRegulation.ClauseID, Is.EqualTo(expectedResultRegulation.ClauseID), $"The ClauseId of {resultRegulation.ClauseID} does not match the expected.");
                    Assert.That(resultRegulation.Subcategory, Is.EqualTo(expectedResultRegulation.Subcategory), $"The Subcategory of {resultRegulation.ClauseID} does not match the expected.");
                    Assert.That(resultRegulation.Description, Is.EqualTo(expectedResultRegulation.Description), $"The Description of {resultRegulation.ClauseID} does not match the expected.");
                    Assert.That(resultRegulation.AttachmentList.Count, Is.EqualTo(expectedResultRegulation.AttachmentList.Count), $"The AttachmentList of {resultRegulation.ClauseID} does not match the expected.");
                    for (int k = 0; k < resultRegulation.AttachmentList.Count; k++)
                    {
                        Assert.That(resultRegulation.AttachmentList[k].ImageName, Is.EqualTo(expectedResultRegulation.AttachmentList[k].ImageName), $"ImageName at position {k} of {resultRegulation.ClauseID} does not match the expected.");
                        Assert.That(resultRegulation.AttachmentList[k].ImageBytes, Is.EqualTo(expectedResultRegulation.AttachmentList[k].ImageBytes), $"ImageBytes at position {k} of {resultRegulation.ClauseID} does not match the expected.");
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

        private bool deleteIssueByKey(string issueKey)
        {
            bool success = false;

            Task t = this.jiraConnectionStub.Issues.DeleteIssueAsync(issueKey);

            while (t.Status == TaskStatus.Running || t.Status == TaskStatus.WaitingForChildrenToComplete || t.Status == TaskStatus.WaitingToRun)
            {
                log.Debug($"Waiting on issue {issueKey} to finish running. Status: {t.Status}");
            }

            success = (t.Status == TaskStatus.RanToCompletion);
            log.Debug($"Task complete. Status: {t.Status}");

            return success;
        }
    }
}