using Atlassian.Jira;
using GliToJiraImporter.Models;
using log4net;
using log4net.Config;
using log4net.Repository;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace GliToJiraImporter.Testing.Tests
{
    public class ParserTester
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Parser sut;
        private ParameterModel parameterModelStub;
        private Jira jiraConnectionStub;
        Project jiraProjectStub;
        private string path;
        IList<CategoryModel> expectedResult;

        [SetUp]
        public void Setup()
        {
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            log.Info(message: new StackFrame().GetMethod().Name);

            path = @"..\..\..\Public\";

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
            parameterModelStub = new()
            {
                FilePath = $"{path}Australia-New-Zealand.docx",
                JiraUrl = "http://localhost:8080/",
                UserName = "",
                Password = "",
                IssueType = "Test Plan",
                SleepTime = 0,
                ProjectKey = "SAM",
                Type = 1,
            };

            this.jiraConnectionStub = Jira.CreateRestClient(parameterModelStub.JiraUrl, parameterModelStub.UserName, parameterModelStub.Password);
            this.jiraProjectStub = jiraConnectionStub.Projects.GetProjectAsync(parameterModelStub.ProjectKey).Result;

            sut = new Parser(this.parameterModelStub, this.jiraConnectionStub);
        }

        //[TearDown]
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
                if (issue["GLICategory"] != null && categories.Contains(issue["GLICategory"].Value) && issue.Labels.Count() == 0)//TODO not good enough
                {
                    bool success = this.deleteIssueByKey(issue.Key.Value);
                    if (success != true)
                    {
                        log.Error($"Issue failed to delete. {issue.Key.Value}");//TODO Not really?
                    }
                }
            }
            expectedResult.Clear();
        }

        [Test]
        public void ParserSingleTest()
        {
            //given
            parameterModelStub.FilePath = $"{path}SINGLE-Australia-New-Zealand.docx";
            expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{path}ParserSingleTestExpectedResult.txt"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            testAssertModel(expectedResult, result);
        }

        [Test]
        public void ParserSingleMultiDescTest()
        {
            //given
            parameterModelStub.FilePath = $"{path}SINGLE-MULTIDESC-Australia-New-Zealand.docx";
            expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{path}ParserSingleMultiDescTestExpectedResult.txt"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            testAssertModel(expectedResult, result);
        }

        [Test]
        public void ParserPicturesTest()
        {
            //given
            parameterModelStub.FilePath = $"{path}PICTURES-SHORT-Australia-New-Zealand.docx";
            expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{path}PicturesTestExpectedResult.txt"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.NotNull(expectedResult);
            testAssertModel(expectedResult, result);
        }

        [Test]
        public void ParserSpecialsTest()
        {
            //given
            parameterModelStub.FilePath = $"{path}SPECIALS-Australia-New-Zealand.docx";
            expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{path}ParserSpecialsTestExpectedResult.txt"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.NotNull(expectedResult);
            testAssertModel(expectedResult, result);
        }

        [Test]
        [Ignore("Work in progress")]
        public void ParserFullTest()
        {
            //given
            //expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{path}FullTestSearchExpectedResult.txt"));
            parameterModelStub.FilePath = $"{path}Australia-New-Zealand.docx";
            int expectedCount = 635;
            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.NotZero(result.Count);
            int totalRegs = 0;
            foreach (CategoryModel category in result)
            {
                totalRegs += category.RegulationList.Count;
            }
            Assert.That(totalRegs, Is.EqualTo(expectedCount));
            //TestAssertModel(expectedResult, result);
        }

        [Test]
        [Ignore("Work in progress")]
        public void ParserUnknownDocTypeTest()
        {
            //given
            parameterModelStub.FilePath = $"{path}SINGLE-Australia-New-Zealand.docx";
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
            //Assert.Catch<Exception>(sut.Parse(),"Unknown document type did not throw an exception.");
        }

        private void testAssertModel(IList<CategoryModel> expectedResult, IList<CategoryModel> result)
        {
            Assert.NotNull(result);
            Assert.That(result.Count, Is.EqualTo(expectedResult.Count));
            for (int i = 0; i < result.Count; i++)
            {
                Assert.That(result[i].Category, Is.EqualTo(expectedResult[i].Category));
                Assert.That(result[i].RegulationList.Count, Is.EqualTo(expectedResult[i].RegulationList.Count));

                for (int j = 0; j < result[i].RegulationList.Count; j++)
                {
                    RegulationModel resultRegulation = (RegulationModel)result[i].RegulationList[j];
                    RegulationModel expectedResultRegulation = (RegulationModel)expectedResult[i].RegulationList[j];
                    Assert.That(resultRegulation.ClauseID, Is.EqualTo(expectedResultRegulation.ClauseID));
                    Assert.That(resultRegulation.Subcategory, Is.EqualTo(expectedResultRegulation.Subcategory));
                    Assert.That(resultRegulation.Description, Is.EqualTo(expectedResultRegulation.Description));
                    Assert.That(resultRegulation.AttachmentList.Count, Is.EqualTo(expectedResultRegulation.AttachmentList.Count));
                    for (int k = 0; k < resultRegulation.AttachmentList.Count; k++)
                    {
                        Assert.That(resultRegulation.AttachmentList[k], Is.EqualTo(expectedResultRegulation.AttachmentList[k]));
                    }
                }
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