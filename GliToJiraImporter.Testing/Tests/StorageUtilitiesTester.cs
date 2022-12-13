using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GliToJiraImporter.Models;
using GliToJiraImporter.Utilities;
using log4net.Appender;
using log4net;
using log4net.Core;
using log4net.Config;
using log4net.Repository;
using RestSharp;
using System.Diagnostics;
using GliToJiraImporter.Testing.Utilities;

namespace GliToJiraImporter.Testing.Tests
{
    public class StorageUtilitiesTester
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        private StorageUtilities sut = new();
        private ParameterModel parameterModelStub = new();
        private MemoryAppender memoryAppender = new();
        private const string ExpectedResultFolderName = @"../../../Public/ExpectedResults/";

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

            this.sut = new StorageUtilities(this.parameterModelStub);
        }

        [Test]
        public void StorageUtilitiesUploadToJiraTest()
        {
            //given
            //parameterModelStub.FilePath = $"{checkoffFolderName}SINGLE-Australia-New-Zealand.docx";
            IList<CategoryModel> categories = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{ExpectedResultFolderName}ParserSingleTestExpectedResult.json"));

            //when
            sut.UploadToJira(categories);

            //then
            this.memoryAppender.AssertNoErrorsInLogs();

            Assert.That(result.Any(), Is.True);
            Assert.That(expectedResult, !Is.Null);
            this.testAssertModel(expectedResult, result);
        }

        [Test]
        public void StorageUtilitiesSingleDuplicateTest()
        {
            //given
            //parameterModelStub.FilePath = $"{checkoffFolderName}SINGLE-Australia-New-Zealand.docx";
            IList<CategoryModel> categories = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{ExpectedResultFolderName}ParserSingleTestExpectedResult.json"));

            //when
            sut.UploadToJira(categories);
            sut.UploadToJira(categories);
            
            //then
            Assert.That(this.memoryAppender.LogExists(Level.Debug, $"Skipping clauseId {categories[0].RegulationList[0].ClauseId.BaseClauseId} because it already exists in the project {parameterModelStub.ProjectKey}")
                , Is.True);
        }
    }
}
