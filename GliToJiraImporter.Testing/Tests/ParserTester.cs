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

        [Test]
        public void ParserSingleTest()
        {
            //given
            parameterModelStub.FilePath = $"{path}SINGLE-Australia-New-Zealand.docx";
            expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{path}ParserSingleTestExpectedResult.txt"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            this.testAssertModel(expectedResult, result);
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
            this.testAssertModel(expectedResult, result);
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
            this.testAssertModel(expectedResult, result);
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
            this.testAssertModel(expectedResult, result);
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
            //this.testAssertModel(expectedResult, result);
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
            Assert.That(result.Count, Is.EqualTo(expectedResult.Count), $"The result count does not match the expected.");
            for (int i = 0; i < result.Count; i++)
            {
                Assert.That(result[i].Category, Is.EqualTo(expectedResult[i].Category), $"The Category of {result[i].Category} does not match the expected.");
                Assert.That(result[i].RegulationList.Count, Is.EqualTo(expectedResult[i].RegulationList.Count), $"The RegulationList count of {result[i].Category} does not match the expected.");

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