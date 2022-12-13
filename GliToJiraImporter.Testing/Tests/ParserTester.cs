using GliToJiraImporter.Models;
using GliToJiraImporter.Parsers;
using GliToJiraImporter.Utilities;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Repository;
using RestSharp;
using System.Diagnostics;
using System.Reflection;
using GliToJiraImporter.Testing.Utilities;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace GliToJiraImporter.Testing.Tests
{
    public class ParserTester
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly string checkoffFolderName = @"../../../Public/TestCheckoffs/";
        private static readonly string expectedResultFolderName = @"../../../Public/ExpectedResults/";
        private Parser sut;
        private ParameterModel parameterModelStub;
        private MemoryAppender memoryAppender;

        [SetUp]
        public void Setup()
        {
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            this.memoryAppender = new log4net.Appender.MemoryAppender();
            BasicConfigurator.Configure(this.memoryAppender);

            log.Info(message: new StackFrame().GetMethod().Name);

            string userName = "samantha.knowlton@everi.com";
            string token = Environment.GetEnvironmentVariable("JIRA_API_TOKEN");
            string userNameToken = $"{userName}:{token}";

            parameterModelStub = new()
            {
                FilePath = $"{checkoffFolderName}Australia-New-Zealand.docx",
                Method = Method.GET,
                JiraUrl = "https://gre-team.atlassian.net",//search?jql=project=EGRE&maxResults=10",
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
            this.testAssertModel(expectedResult, result);
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
            this.testAssertModel(expectedResult, result);
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
            this.testAssertModel(expectedResult, result);
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
            this.testAssertModel(expectedResult, result);
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
            //this.testAssertModel(expectedResult, result);
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
            this.testAssertModel(expectedResult, result);
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
            this.testAssertModel(expectedResult, result);
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
            this.testAssertModel(expectedResult, result);
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
            this.testAssertModel(expectedResult, result);
        }

        private void testAssertModel(IList<CategoryModel> expectedResult, IList<CategoryModel> result)
        {
            this.memoryAppender.AssertNoErrorsInLogs();
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
                    Assert.That(resultRegulation.ClauseId.BaseClauseId, Is.EqualTo(expectedResultRegulation.ClauseId.BaseClauseId), $"The BaseClauseId of {resultRegulation.ClauseId.BaseClauseId} does not match the expected.");
                    Assert.That(resultRegulation.ClauseId.FullClauseId, Is.EqualTo(expectedResultRegulation.ClauseId.FullClauseId), $"The FullClauseId of {resultRegulation.ClauseId.FullClauseId} does not match the expected.");
                    Assert.That(resultRegulation.Subcategory, Is.EqualTo(expectedResultRegulation.Subcategory), $"The Subcategory of {resultRegulation.ClauseId.FullClauseId} does not match the expected.");
                    Assert.That(resultRegulation.Description.Text, Is.EqualTo(expectedResultRegulation.Description.Text), $"The Description Text of {resultRegulation.ClauseId.FullClauseId} does not match the expected.");

                    IList<PictureModel> resultAttachmentList = ((DescriptionModel)resultRegulation.Description).AttachmentList;
                    IList<PictureModel> expectedResultAttachmentList = ((DescriptionModel)expectedResultRegulation.Description).AttachmentList;
                    Assert.That(resultAttachmentList.Count, Is.EqualTo(expectedResultAttachmentList.Count), $"The AttachmentList of {resultRegulation.ClauseId.FullClauseId} does not match the expected.");
                    for (int k = 0; k < resultAttachmentList.Count; k++)
                    {
                        Assert.That(resultAttachmentList[k].ImageName, Is.EqualTo(expectedResultAttachmentList[k].ImageName), $"ImageName at position {k} of {resultRegulation.ClauseId.FullClauseId} does not match the expected.");
                        Assert.That(resultAttachmentList[k].ImageBytes, Is.EqualTo(expectedResultAttachmentList[k].ImageBytes), $"ImageBytes at position {k} of {resultRegulation.ClauseId.FullClauseId} does not match the expected.");
                    }
                }
            }
        }
    }
}