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

        private string path;

        [SetUp]
        public void Setup()
        {
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            log.Info(message: new StackFrame().GetMethod().Name);

            path = @"..\..\..\Public\";

            parameterModelStub = new()
            {
                //FileName = @"C:\MForce\GliToJiraImporter\GliToJiraImporter.Testing\Public\Australia-New-Zealand.docx",
                FileName = $"{path}Australia-New-Zealand.docx",
                Type = 1
            };

            sut = new Parser(parameterModelStub);
        }

        //[Test]
        public void ParserTest()
        {
            //given
            //TODO Create result? Remove? I guess decide how to deal with this test
            string result = "";

            //when
            sut.Parse();

            //then
            //Assert.That(result, Is.EqualTo(true), "Invalid Data Model");
        }

        [Test]
        public void ParserSingleTest()
        {
            //given
            parameterModelStub.FileName = $"{path}SINGLE-Australia-New-Zealand.docx";
            List<CategoryModel> expectedResult = new List<CategoryModel>()
            {
                new CategoryModel()
                {
                    Category = "Cabinet",
                    RegulationList = new List<RegulationModel>()
                    {
                        new RegulationModel()
                        {
                            ClauseID = "NS2.3.2",
                            Subcategory = "Cabinet Identification",
                            Description = "The ID badge is to be fixed on the exterior of the gaming machine in a position that allows it to be easily read."
                        }
                    }
                }
            };

            //when
            List<CategoryModel> result = sut.Parse();

            //then
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].RegulationList.Count, Is.EqualTo(1));
            TestAssertModel(expectedResult, result);
        }

        [Test]
        public void ParserSingleMultiDescTest()
        {
            //given
            parameterModelStub.FileName = $"{path}SINGLE-MULTIDESC-Australia-New-Zealand.docx";
            List<CategoryModel> expectedResult = new List<CategoryModel>()
            {
                new CategoryModel()
                {
                    Category = "Cabinet",
                    RegulationList = new List<RegulationModel>()
                    {
                        new RegulationModel()
                        {
                            ClauseID = "NS2.3.1",
                            Subcategory = "Cabinet Identification",
                            Description = "A gaming machine must have an identification badge permanently affixed to its cabinet by the manufacturer, and this badge must include the following information:" +
                            "\na) the manufacturer;" +
                            "\nb) a unique serial number;" +
                            "\nc) the gaming machine model number; " +
                            "\nd) the date of manufacture."
                        }
                    }
                }
            };

            //when
            List<CategoryModel> result = sut.Parse();

            //then
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].RegulationList.Count, Is.EqualTo(1));
            TestAssertModel(expectedResult, result);
        }

        [Test]
        public void ParserPicturesTest()
        {
            //given
            parameterModelStub.FileName = $"{path}PICTURES-SHORT-Australia-New-Zealand.docx";
            List<CategoryModel> expectedResult = new List<CategoryModel>()
            {
                new CategoryModel()
                {
                    Category = "Spinning Reel Games",
                    RegulationList = new List<RegulationModel>()
                    {
                        new RegulationModel()
                        {
                            ClauseID = "NS4.3.24",
                            Subcategory = "Lit Lines",
                            Description = "Where winning patterns are paid on lit lines only, the artwork must include the statement \"All wins on lit lines only except [X] [Y] and [Z]\" where [X] [Y] and [Z] are the exceptions to this rule (e.g.. scatters, feature wins etc.)",
                            AttachmentList = new List<byte[]>()
                        },
                        new RegulationModel()
                        {
                            ClauseID = "NS4.3.25",
                            Subcategory = "Lit Lines",
                            Description = "4.3.25, 4.3.26 and 4.3.27 refer to games with 5 reels, and 3 rows of symbols\nGames consisting of 1 line must contain the following line: ",
                            AttachmentList = new List<byte[]>() { new byte[]{} } //TODO Add the image
                        },
                        new RegulationModel()
                        {
                            ClauseID = "NS4.3.26",
                            Subcategory = "Lit Lines",
                            Description = "Games consisting of 3 lines must contain the following lines, numbered as: ",
                            AttachmentList = new List<byte[]>() { new byte[]{} } //TODO Add the image
                        },
                        new RegulationModel()
                        {
                            ClauseID = "NS4.3.27",
                            Subcategory = "Lit Lines",
                            Description = "Games consisting of 5 lines must contain the following lines, numbered as: ",
                            AttachmentList = new List<byte[]>() { new byte[]{} } //TODO Add the image
                        },
                    }
                }
            };

            //when
            List<CategoryModel> result = sut.Parse();

            //then
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].RegulationList.Count, Is.EqualTo(4));
            TestAssertModel(expectedResult, result);
        }

        [Test]
        public void ParserSpecialsTest()
        {
            //given
            parameterModelStub.FileName = $"{path}SPECIALS-Australia-New-Zealand.docx";
            List<CategoryModel> expectedResult = ReadFile($"{path}ParserSpecialsTestExpectedResult.txt");
            //new List<CategoryModel>()

            //when
            List<CategoryModel> result = sut.Parse();

            //then
            Assert.That(result.Count, Is.EqualTo(17), $"Expected Categories: {17}, but was: {result.Count}");

            int totalRegs = 0;
            foreach (CategoryModel category in result)
            {
                log.Debug($"{category.Category} - {category.RegulationList.Count}");
                totalRegs += category.RegulationList.Count;
            }

            Assert.That(totalRegs, Is.EqualTo(76));
            TestAssertModel(expectedResult, result);
        }

        [Test]
        public void ParserFullTest()
        {
            //given
            //List<CategoryModel> expectedResult = ReadFile($"{path}ParserFullTestExpectedResult.txt");

            parameterModelStub.FileName = $"{path}Australia-New-Zealand.docx";

            //when
            List<CategoryModel> result = sut.Parse();

            //then
            Assert.That(result.Count, !Is.EqualTo(0));
            int totalRegs = 0;
            foreach (CategoryModel category in result)
            {
                totalRegs += category.RegulationList.Count;
            }
            Assert.That(totalRegs, Is.EqualTo(635));
            //TestAssertModel(expectedResult, result);
        }

        private void TestAssert(string expectedResult)
        {
            string result;
            using (var reader = new StreamReader(@"..\..\..\Public\Result.txt"))
            {
                result = reader.ReadToEnd();
            }

            Assert.That(result, Is.EqualTo(expectedResult), $"Invalid Data Model; Received: {result}");
        }

        private void TestAssertModel(List<CategoryModel> expectedResult, List<CategoryModel> result)
        {
            Assert.That(result.Count, Is.EqualTo(expectedResult.Count));
            for (int i = 0; i < result.Count; i++)
            {
                Assert.That(result[i].Category, Is.EqualTo(expectedResult[i].Category));
                Assert.That(result[i].RegulationList.Count, Is.EqualTo(expectedResult[i].RegulationList.Count));

                for (int j = 0; j < result[i].RegulationList.Count; j++)
                {
                    Assert.That(result[i].RegulationList[j].ClauseID, Is.EqualTo(expectedResult[i].RegulationList[j].ClauseID));
                    Assert.That(result[i].RegulationList[j].Subcategory, Is.EqualTo(expectedResult[i].RegulationList[j].Subcategory));
                    Assert.That(result[i].RegulationList[j].Description, Is.EqualTo(expectedResult[i].RegulationList[j].Description));
                    Assert.That(result[i].RegulationList[j].AttachmentList.Count, Is.EqualTo(expectedResult[i].RegulationList[j].AttachmentList.Count)); // TODO Not a good enough check
                }
            }
        }

        private List<CategoryModel> ReadFile(string path)
        {
            List<CategoryModel> result = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText(path));
            //try
            //{
            //    // Check if file already exists. If yes, delete it.     
            //    if (File.Exists(path))
            //    {
            //        using (FileStream fs = File.Open(path))
            //        {
                        
            //            // Add some text to file    
            //            Byte[] textBytes = new UTF8Encoding(true).GetBytes(text);
            //            fs.Read();
            //            List<CategoryModel> result = JsonSerializer.Deserialize<List<CategoryModel>>(File.ReadAllText(path));
            //        }
            //    }

            //    // Create a new file     
            //}
            //catch (Exception Ex)
            //{
            //    Console.WriteLine(Ex.ToString());
            //}

            return result;
        }
    }
}