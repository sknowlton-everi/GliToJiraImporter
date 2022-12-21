using System.Reflection;
using System.Text.Json;
using GliToJiraImporter.Models;
using GliToJiraImporter.Utilities;
using log4net.Appender;
using log4net;
using log4net.Core;
using log4net.Config;
using log4net.Repository;
using RestSharp;
using System.Diagnostics;
using GliToJiraImporter.Testing.Extensions;

namespace GliToJiraImporter.Testing.Tests
{
    public class StorageUtilitiesTester
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        private StorageUtilities sut = new();
        private ParameterModel parameterModelStub = new();
        private readonly MemoryAppender memoryAppender = new();
        private const string ExpectedResultFolderName = @"../../../Public/ExpectedResults/";

        [SetUp]
        public void Setup()
        {
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
            BasicConfigurator.Configure(this.memoryAppender);

            log.Info(message: new StackFrame().GetMethod()?.Name);

            const string userName = "samantha.knowlton@everi.com";
            string token = Environment.GetEnvironmentVariable("JIRA_API_TOKEN");
            string userNameToken = $"{userName}:{token}";

            this.parameterModelStub = new ParameterModel
            {
                FilePath = @"../../../Public/TestCheckoffs/SINGLE-Australia-New-Zealand.docx",
                Method = Method.GET,
                JiraUrl = "https://gre-team.atlassian.net/rest/api/2",
                UserName = userNameToken,
                IssueType = "Test Plan",
                SleepTime = 1000,
                ProjectKey = "EGRE",
                Type = 1,
            };

            this.sut = new StorageUtilities(this.parameterModelStub);
        }

        [Test]
        public void StorageUtilitiesSingleDuplicateTest()
        {
            //given
            //parameterModelStub.FilePath = $"{checkoffFolderName}SINGLE-Australia-New-Zealand.docx";
            IList<CategoryModel> categories = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{ExpectedResultFolderName}ParserSingleTestExpectedResult.json"));

            //when
            this.sut.UploadToJira(categories);
            this.sut.UploadToJira(categories);
            
            //then
            Assert.That(this.memoryAppender.LogExists(Level.Debug, $"Skipping clauseId {categories[0].RegulationList[0].ClauseId.BaseClauseId} because it already exists in the project {parameterModelStub.ProjectKey}")
                , Is.True);
        }
    }
}
