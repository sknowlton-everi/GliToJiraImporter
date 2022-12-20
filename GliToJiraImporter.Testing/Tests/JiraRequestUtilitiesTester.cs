using GliToJiraImporter.Models;
using GliToJiraImporter.Utilities;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository;
using RestSharp;
using System.Diagnostics;
using System.Reflection;
using GliToJiraImporter.Testing.Extensions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace GliToJiraImporter.Testing.Tests
{
    internal class JiraRequestUtilitiesTester
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        private JiraRequestUtilities sut = new();
        private ParameterModel parameterModelStub = new();
        private MemoryAppender memoryAppender = new();
        private const string checkoffFolderName = @"../../../Public/TestCheckoffs/";
        private const string expectedResultFolderName = @"../../../Public/ExpectedResults/";
        private CategoryModel expectedResult = new();
        private JiraIssue jiraIssue = new();

        [OneTimeSetUp]
        public void Init()
        {
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
            BasicConfigurator.Configure(this.memoryAppender);

            log.Info(message: new StackFrame().GetMethod().Name);

            string userName = "samantha.knowlton@everi.com";
            string token = Environment.GetEnvironmentVariable("JIRA_API_TOKEN");
            string userNameToken = $"{userName}:{token}";

            this.parameterModelStub = new()
            {
                FilePath = checkoffFolderName + @"SINGLE-Australia-New-Zealand.docx",
                Method = Method.GET,
                JiraUrl = "https://gre-team.atlassian.net/rest/api/2",
                UserName = userNameToken,
                IssueType = "Test Plan",
                SleepTime = 1000,
                ProjectKey = "EGRE",
                Type = 1,
            };

            string expectedResultPath = $"{expectedResultFolderName}ParserSingleTestExpectedResult.json";
            this.expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText(expectedResultPath)).First();
            this.jiraIssue = new JiraIssue(this.parameterModelStub.ProjectKey, "Test Plan",
                this.expectedResult.RegulationList[0].ClauseId.FullClauseId, this.expectedResult.RegulationList[0].ClauseId.FullClauseId,
                this.expectedResult.Category, this.expectedResult.RegulationList[0].Subcategory, this.expectedResult.RegulationList[0].Description.Text);

            this.sut = new JiraRequestUtilities(this.parameterModelStub);
        }

        [Test, Order(1)]
        public void JiraPostIssueTest()
        {
            //when
            bool result = this.sut.PostIssue(this.jiraIssue);

            //then
            Assert.That(result, Is.True);
            this.memoryAppender.AssertNoErrorsInLogs();

            //expectedResult.TestAssertModel(result);
        }

        [Test, Order(3)]
        public void JiraPostAttachmentToIssueByKeyTest()
        {
            //when
            string issueKey = this.sut.GetIssueByClauseId(this.jiraIssue.fields.customfield_10046).key;
            bool result = this.sut.PostAttachmentToIssueByKey(this.jiraIssue, @"../../../Public/Picture-2.png", issueKey);
            PictureModel pictureModel = new();
            pictureModel.ImageName = "Picture-2.png";
            pictureModel.ImageBytes = File.ReadAllBytes(@"../../../Public/Picture-2.png");
            this.expectedResult.RegulationList[0].Description.AttachmentList.Add(pictureModel);
            //then
            Assert.That(result, Is.True);
            this.memoryAppender.AssertNoErrorsInLogs();

            //expectedResult.TestAssertModel(result);
        }

        [Test, Order(7)]
        public void JiraGetAllIssuesWithAClauseIdTest()
        {
            //when
            IList<Issue> result = this.sut.GetAllIssuesWithAClauseId();

            //then
            Assert.That(result, !Is.Null);
            this.memoryAppender.AssertNoErrorsInLogs();

            for (int i = 0; i < result.Count; i++)
            {
                Assert.That(result[i].fields.customfield_10045, !Is.EqualTo(this.expectedResult.RegulationList[0].ClauseId.FullClauseId));
            }
        }

        [Test, Order(2)]
        public void JiraGetIssueByClauseIdTest()
        {
            //when
            Issue result = this.sut.GetIssueByClauseId(this.expectedResult.RegulationList[0].ClauseId.FullClauseId);

            //then
            Assert.That(result, !Is.Null);
            this.memoryAppender.AssertNoErrorsInLogs();

            this.expectedResult.TestAssertCategoryModel(this.issueToCategoryModel(result));
        }

        [Test, Order(5)]
        public void JiraGetIssueByIssueKeyTest()
        {
            //when
            string issueKey = this.sut.GetIssueByClauseId(this.expectedResult.RegulationList[0].ClauseId.FullClauseId).key;
            Issue result = this.sut.GetIssueByIssueKey(issueKey);

            //then
            Assert.That(result, !Is.Null);
            this.memoryAppender.AssertNoErrorsInLogs();

            this.expectedResult.TestAssertCategoryModel(this.issueToCategoryModel(result));
        }

        [Test, Order(4)]
        public void JiraGetIssueAttachmentByIdTest()
        {
            //when
            Issue issue = sut.GetIssueByClauseId(this.expectedResult.RegulationList[0].ClauseId.FullClauseId);
            byte[] result = this.sut.GetIssueAttachmentById(issue.fields.attachment[0].id);

            //then
            Assert.That(result, !Is.Null);
            this.memoryAppender.AssertNoErrorsInLogs();

            Assert.That(result, Is.EqualTo(this.expectedResult.RegulationList[0].Description.AttachmentList[0].ImageBytes));
        }

        [Test, Order(6)]
        public void JiraDeleteIssueByKeyTest()
        {
            //when
            string issueKey = this.sut.GetIssueByClauseId(this.jiraIssue.fields.customfield_10046).key;
            bool result = this.sut.DeleteIssueByKey(issueKey);

            //then
            Assert.That(result, Is.True);
            this.memoryAppender.AssertNoErrorsInLogs();
        }

        private CategoryModel issueToCategoryModel(Issue issue)
        {
            CategoryModel result = new();
            result.Category = (string)issue.fields.customfield_10044;


            DescriptionModel descriptionModel = new();
            descriptionModel.Text = issue.fields.description;
            foreach (Attachment attachment in issue.fields.attachment)
            {
                PictureModel picture = new();
                picture.ImageName = attachment.filename;
                picture.ImageBytes = this.expectedResult.RegulationList[0].Description.AttachmentList[0].ImageBytes;
                descriptionModel.AttachmentList.Add(picture);
            }

            ClauseIdModel clauseId = new();
            clauseId.FullClauseId = issue.fields.customfield_10046;
            clauseId.BaseClauseId = this.expectedResult.RegulationList[0].ClauseId.BaseClauseId;

            RegulationModel regulationModel = new();
            regulationModel.Description = descriptionModel;
            regulationModel.ClauseId = clauseId;
            regulationModel.Subcategory = (string)issue.fields.customfield_10045;

            result.RegulationList.Add(regulationModel);

            return result;
        }
    }
}
