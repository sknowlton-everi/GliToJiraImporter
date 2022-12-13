using GliToJiraImporter.Models;
using GliToJiraImporter.Utilities;
using log4net;
using System.Reflection;
using GliToJiraImporter.Parsers;

namespace GliToJiraImporter
{
    /// <summary>
    /// This is a Singleton class
    /// </summary>
    public class UnitOfWork
    {
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        private static readonly UnitOfWork instance = new();

        protected UnitOfWork() {}

        public static UnitOfWork Instance()
        {
            return instance;
        }

        public bool Execute(ParameterModel parameterModel)
        {
            try
            {
                //Parse
                Parser parser = new(parameterModel);
                IList<CategoryModel> parsedCategoryModels = parser.Parse();

                //Upload
                StorageUtilities storageUtilities = new(parameterModel);
                storageUtilities.UploadToJira(parsedCategoryModels);
                // Uncomment if you want to save results to the public folder in the test project
                //this.storageUtilities.SaveText(@"..\..\..\..\GliToJiraImporter.Testing\Public\Results.txt", JsonSerializer.Serialize(result));
                //this.storageUtilities.SaveCsv(@"..\..\..\..\GliToJiraImporter.Testing\Public\ResultsCsv.csv", result);

                //Verify
                if (!storageUtilities.VerifyCategoryModelsExistInJira(parsedCategoryModels))
                {
                    this.log.Error("The results were invalid.");
                    return false;
                }
            }
            catch (InvalidCastException e)
            {
                this.log.Error(e);
                return false;
            }
            catch (Exception e)
            {
                this.log.Error(e);
            }

            return true;
        }
    }
}
