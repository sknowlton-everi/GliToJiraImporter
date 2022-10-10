using Atlassian.Jira;
using GliToJiraImporter.Models;
using log4net;
using log4net.Config;
using log4net.Repository;
using System.Diagnostics;
using System.Reflection;

namespace GliToJiraImporter
{
    public class Program
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Main(string[] args)
        {
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            log.Debug("Starting Application");
            log.Info(new StackFrame().GetMethod().Name);

            try
            {
                ParameterModel parameterModel = parseCommandLine(args);
                Jira jiraConnection = Jira.CreateRestClient(parameterModel.JiraUrl, parameterModel.UserName, parameterModel.Password);
                Parser parser = new(parameterModel, jiraConnection);
                parser.Parse();
                //parser.ParseMementos();
            }
            catch (Exception e)
            {
                log.Error(e);
            }

            log.Debug("Ending Application");
            Console.ReadKey();
        }

        private static ParameterModel parseCommandLine(string[] args)
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
    }
}