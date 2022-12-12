using GliToJiraImporter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GliToJiraImporter.Utilities;
using log4net;
using System.Reflection;

namespace GliToJiraImporter
{
    /// <summary>
    /// This is a Singleton class
    /// </summary>
    public class UnitOfWork
    {
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static UnitOfWork instance;

        protected UnitOfWork(){}

        public static UnitOfWork Instance()
        {
            if (instance == null)
            {
                instance = new UnitOfWork();
            }
            return instance;
        }

        public bool Execute(ParameterModel parameterModel)
        {
            bool result = false;

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
                if (storageUtilities.VerifyCategoryModelsExistInJira(parsedCategoryModels))
                {
                    log.Error("The results were invalid.");
                }
            }
            catch (Exception e)
            {
                //TODO specify exceptions to catch. Do not leave his generic exception
            }

            return result;
        }
    }
}
