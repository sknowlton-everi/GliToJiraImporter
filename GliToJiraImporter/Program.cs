using GliToJiraImporter.Models;
using GliToJiraImporter.Utilities;
using log4net;
using log4net.Config;
using log4net.Repository;
using System.Diagnostics;
using System.Reflection;

namespace GliToJiraImporter
{
    public class Program
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);

        public static void Main(string[] args)
        {
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            log.Debug("Starting Application");
            log.Info(new StackFrame().GetMethod()?.Name);

            try
            {
                ParameterModel parameterModel = parseCommandLine(args);
                parameterModel.UserName = $"{parameterModel.UserName}:{Environment.GetEnvironmentVariable("JIRA_API_TOKEN")}";
                Parser parser = new(parameterModel);
                IList<CategoryModel> parsedCategoryModels = parser.Parse();

                StorageUtilities storageUtilities = new (parameterModel);
                storageUtilities.UploadToJira(parsedCategoryModels);
                // Uncomment if you want to save results to the public folder in the test project
                //this.storageUtilities.SaveText(@"..\..\..\..\GliToJiraImporter.Testing\Public\Results.txt", JsonSerializer.Serialize(result));
                //this.storageUtilities.SaveCsv(@"..\..\..\..\GliToJiraImporter.Testing\Public\ResultsCsv.csv", result);

                //TODO verify
            }
            catch (Exception e)
            {
                log.Error(e);
            }

            log.Debug("Ending Application");
            Console.ReadKey();
        }

        private static ParameterModel parseCommandLine(IEnumerable<string> args)
        {
            CommandLine.ParserResult<ParameterModel> commandLineArgs = CommandLine.Parser.Default.ParseArguments<ParameterModel>(args);

            if (commandLineArgs.Tag == CommandLine.ParserResultType.NotParsed)
            {
                log.Debug("No processing happened.  Invalid command line parameters.  Press any key to continue");
            }

            ParameterModel result = (commandLineArgs as CommandLine.Parsed<ParameterModel>).Value;
            if (File.Exists(result.FilePath) == false)
            {
                log.Debug($"No processing happened.  Unable to find the specified file:  {result.FilePath}");
            }

            return result;
        }

        private bool verifyResults()
        {


            return false;
        }
    }
}