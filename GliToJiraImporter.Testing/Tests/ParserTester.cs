using GliToJiraImporter.Models;
using GliToJiraImporter.Parsers;
using GliToJiraImporter.Testing.Extensions;
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
    public class ParserTester
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly string checkoffFolderName = @"../../../Public/TestCheckoffs/";
        private static readonly string expectedResultFolderName = @"../../../Public/ExpectedResults/";
        private Parser sut;
        private ParameterModel parameterModelStub = new();
        private readonly MemoryAppender memoryAppender = new();

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
                FilePath = $"{checkoffFolderName}Australia-New-Zealand.docx",
                Method = Method.GET,
                JiraUrl = "https://gre-team.atlassian.net/rest/api/2",//search?jql=project=EGRE&maxResults=10",
                UserName = userNameToken,
                IssueType = "Test Plan",
                SleepTime = 1000,
                ProjectKey = "EGRE",
                Type = 1,
            };

            sut = new Parser(this.parameterModelStub);
        }

        [Test]
        public void ParserSingleTest()
        {
            //given
            parameterModelStub.FilePath = $"{checkoffFolderName}SINGLE-Australia-New-Zealand.docx";
            string expectedResultPath = $"{expectedResultFolderName}ParserSingleTestExpectedResult.json";
            IList<CategoryModel> expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText(expectedResultPath));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            expectedResult.TestAssertCategoryModels(result);
        }

        [Test]
        public void ParserSingleMultiDescTest()
        {
            //given
            parameterModelStub.FilePath = $"{checkoffFolderName}SINGLE-MULTIDESC-Australia-New-Zealand.docx";
            string expectedResultPath = $"{expectedResultFolderName}ParserSingleMultiDescTestExpectedResult.json";
            IList<CategoryModel> expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText(expectedResultPath));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            expectedResult.TestAssertCategoryModels(result);
        }

        [Test]
        public void ParserPicturesTest()
        {
            //given
            parameterModelStub.FilePath = $"{checkoffFolderName}PICTURES-SHORT-Australia-New-Zealand.docx";
            IList<CategoryModel> expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{expectedResultFolderName}PicturesTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.That(expectedResult, !Is.Null);
            expectedResult.TestAssertCategoryModels(result);
        }

        //[Ignore("Does not work due to more then 50 tasks")]
        [Test]
        public void ParserSpecialsTest()
        {
            //given
            parameterModelStub.FilePath = $"{checkoffFolderName}SPECIALS-Australia-New-Zealand.docx";
            IList<CategoryModel> expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{expectedResultFolderName}ParserSpecialsTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.That(expectedResult, !Is.Null);
            expectedResult.TestAssertCategoryModels(result);
        }

        [Ignore("This test is very large and takes a long time to complete.")]
        [Test]
        public void ParserFullTest()
        {
            //given
            //IList<CategoryModel> expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{expectedResultFolderName}FullTestSearchExpectedResult.json"));
            parameterModelStub.FilePath = $"{checkoffFolderName}Australia-New-Zealand.docx";
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
            //expectedResult.TestAssertCategoryModels(result);
        }

        [Test]
        public void ParserUnknownDocTypeTest()
        {
            //given
            parameterModelStub.FilePath = $"{checkoffFolderName}SINGLE-Australia-New-Zealand.docx";
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

        [Test]
        public void ParserCharFormatTest()
        {
            //given
            parameterModelStub.FilePath = $"{checkoffFolderName}CHARFORMAT-Australia-New-Zealand.docx";
            IList<CategoryModel> expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{expectedResultFolderName}ParserCharFormatTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.That(expectedResult, !Is.Null);
            expectedResult.TestAssertCategoryModels(result);
        }

        [Test]
        public void ParserClauseIdVarietiesTest()
        {
            //given
            parameterModelStub.FilePath = $"{checkoffFolderName}CLAUSEID-VARIETIES.docx";
            IList<CategoryModel> expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{expectedResultFolderName}ParserClauseIdVarietiesTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.That(expectedResult, !Is.Null);
            expectedResult.TestAssertCategoryModels(result);
        }

        [Test]
        public void ParserNoCategoryTest()
        {
            //given
            parameterModelStub.FilePath = $"{checkoffFolderName}NO-CATEGORY-Australia-New-Zealand.docx";
            IList<CategoryModel> expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{expectedResultFolderName}ParserNoCategoryTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.That(expectedResult, !Is.Null);
            expectedResult.TestAssertCategoryModels(result);
        }

        [Test]
        public void ParserLinkTest()
        {
            //given
            parameterModelStub.FilePath = $"{checkoffFolderName}LINKS-Australia-New-Zealand.docx";
            IList<CategoryModel> expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{expectedResultFolderName}ParserLinkTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = sut.Parse();

            //then
            Assert.That(expectedResult, !Is.Null);
            expectedResult.TestAssertCategoryModels(result);
        }
    }
}