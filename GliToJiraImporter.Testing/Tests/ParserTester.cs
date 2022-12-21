using GliToJiraImporter.Models;
using GliToJiraImporter.Parsers;
using GliToJiraImporter.Testing.Extensions;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository;
using RestSharp;
using System.Diagnostics;
using System.Reflection;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace GliToJiraImporter.Testing.Tests
{
    public class ParserTester
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);

        private const string CheckoffFolderName = @"../../../Public/TestCheckoffs/";
        private const string ExpectedResultFolderName = @"../../../Public/ExpectedResults/";
        private Parser sut;
        private ParameterModel parameterModelStub = new();
        private readonly MemoryAppender memoryAppender = new();

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
                FilePath = $"{CheckoffFolderName}Australia-New-Zealand.docx",
                Method = Method.GET,
                JiraUrl = "https://gre-team.atlassian.net/rest/api/2",//search?jql=project=EGRE&maxResults=10",
                UserName = userNameToken,
                IssueType = "Test Plan",
                SleepTime = 1000,
                ProjectKey = "EGRE",
                Type = 1,
            };

            this.sut = new Parser(this.parameterModelStub);
        }

        [Test]
        public void ParserSingleTest()
        {
            //given
            this.parameterModelStub.FilePath = $"{CheckoffFolderName}SINGLE-Australia-New-Zealand.docx";
            const string expectedResultPath = $"{ExpectedResultFolderName}ParserSingleTestExpectedResult.json";
            IList<CategoryModel> expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText(expectedResultPath));

            //when
            IList<CategoryModel> result = this.sut.Parse();

            //then
            expectedResult.TestAssertCategoryModels(result);
        }

        [Test]
        public void ParserSingleMultiDescTest()
        {
            //given
            this.parameterModelStub.FilePath = $"{CheckoffFolderName}SINGLE-MULTIDESC-Australia-New-Zealand.docx";
            const string expectedResultPath = $"{ExpectedResultFolderName}ParserSingleMultiDescTestExpectedResult.json";
            IList<CategoryModel> expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText(expectedResultPath));

            //when
            IList<CategoryModel> result = this.sut.Parse();

            //then
            expectedResult.TestAssertCategoryModels(result);
        }

        [Test]
        public void ParserPicturesTest()
        {
            //given
            this.parameterModelStub.FilePath = $"{CheckoffFolderName}PICTURES-SHORT-Australia-New-Zealand.docx";
            IList<CategoryModel> expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{ExpectedResultFolderName}PicturesTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = this.sut.Parse();

            //then
            Assert.That(expectedResult, !Is.Null);
            expectedResult.TestAssertCategoryModels(result);
        }

        //[Ignore("Does not work due to more then 50 tasks")]
        [Test]
        public void ParserSpecialsTest()
        {
            //given
            this.parameterModelStub.FilePath = $"{CheckoffFolderName}SPECIALS-Australia-New-Zealand.docx";
            IList<CategoryModel> expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{ExpectedResultFolderName}ParserSpecialsTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = this.sut.Parse();

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
            this.parameterModelStub.FilePath = $"{CheckoffFolderName}Australia-New-Zealand.docx";
            const int expectedCount = 633;
            //when
            IList<CategoryModel> result = this.sut.Parse();

            //then
            Assert.That(result.Count, !Is.EqualTo(0));
            int totalRegulations = result.Sum(categoryModel => categoryModel.RegulationList.Count);
            Assert.That(totalRegulations, Is.EqualTo(expectedCount));
            //expectedResult.TestAssertCategoryModels(result);
        }

        [Test]
        public void ParserUnknownDocTypeTest()
        {
            //given
            this.parameterModelStub.FilePath = $"{CheckoffFolderName}SINGLE-Australia-New-Zealand.docx";
            this.parameterModelStub.Type = 0;
            IList<CategoryModel> result = new List<CategoryModel>();
            //when
            try
            {
                result = this.sut.Parse();
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
            this.parameterModelStub.FilePath = $"{CheckoffFolderName}CHARFORMAT-Australia-New-Zealand.docx";
            IList<CategoryModel> expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{ExpectedResultFolderName}ParserCharFormatTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = this.sut.Parse();

            //then
            Assert.That(expectedResult, !Is.Null);
            expectedResult.TestAssertCategoryModels(result);
        }

        [Test]
        public void ParserClauseIdVarietiesTest()
        {
            //given
            this.parameterModelStub.FilePath = $"{CheckoffFolderName}CLAUSEID-VARIETIES.docx";
            IList<CategoryModel> expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{ExpectedResultFolderName}ParserClauseIdVarietiesTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = this.sut.Parse();

            //then
            Assert.That(expectedResult, !Is.Null);
            expectedResult.TestAssertCategoryModels(result);
        }

        [Test]
        public void ParserNoCategoryTest()
        {
            //given
            this.parameterModelStub.FilePath = $"{CheckoffFolderName}NO-CATEGORY-Australia-New-Zealand.docx";
            IList<CategoryModel> expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{ExpectedResultFolderName}ParserNoCategoryTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = this.sut.Parse();

            //then
            Assert.That(expectedResult, !Is.Null);
            expectedResult.TestAssertCategoryModels(result);
        }

        [Test]
        public void ParserLinkTest()
        {
            //given
            this.parameterModelStub.FilePath = $"{CheckoffFolderName}LINKS-Australia-New-Zealand.docx";
            IList<CategoryModel> expectedResult = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText($"{ExpectedResultFolderName}ParserLinkTestExpectedResult.json"));

            //when
            IList<CategoryModel> result = this.sut.Parse();

            //then
            Assert.That(expectedResult, !Is.Null);
            expectedResult.TestAssertCategoryModels(result);
        }
    }
}