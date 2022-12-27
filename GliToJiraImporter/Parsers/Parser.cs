using GliToJiraImporter.Models;
using GliToJiraImporter.Types;
using log4net;
using Syncfusion.DocIO.DLS;
using System.Diagnostics;
using System.Reflection;

namespace GliToJiraImporter.Parsers
{
    public class Parser
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        private readonly ParameterModel parameterModel;

        public Parser(ParameterModel parameterModel)
        {
            log.Info(new StackFrame().GetMethod()?.Name);
            this.parameterModel = parameterModel;
        }

        public IList<CategoryModel> Parse()
        {
            log.Debug("Checking doc type...");
            IList<CategoryModel> result;
            if (this.parameterModel.Type == (int)DocumentType.CHECK_OFF)
            {
                log.Debug($"Document Type: {this.parameterModel.GetType().Name}");
                log.Debug("Parsing...");
                result = this.parseMementos();
            }
            else
            {
                log.Debug($"Document Type Unknown: {this.parameterModel.Type}");
                // Log and throw unknown type
                throw new Exception("Provided Document Type is Unknown. Try type 1 for Checkoff documents.");//TODO is this okay? I don't like the vagueness of type 'Exception'
            }
            log.Debug($"Completed list size: {result.Count}");

            return result;
        }

        private IList<CategoryModel> parseMementos()
        {
            IList<CategoryModel> result = new List<CategoryModel>();

            // Originator and Caretaker instantiation
            CategoryParser categoryOriginator = new();
            Caretaker caretaker = new(categoryOriginator);

            // Creates an instance of WordDocument class
            WSection section = this.getDocumentFromPath().Sections[0];

            WTextBody? documentBody = null;
            // Iterates the tables of the section
            for (int i = 0; i < section.ChildEntities.Count; i++)
            {
                if (section.ChildEntities[i].GetType() == typeof(WTextBody))
                {
                    documentBody = (WTextBody)section.ChildEntities[i];
                }
            }

            if (documentBody == null)
            {
                //Throw exception
                const string errorMessage = "No text body was found in the document.";
                log.Error(errorMessage);
                throw new Exception(errorMessage);
            }

            // Get the main table starting index
            int j = this.getMainTableIndex(documentBody.ChildEntities);
            if (j < 0)
            {
                //Throw exception
                const string errorMessage = "No paragraph with \"Jurisdictional Requirements\" was found.";
                log.Error(errorMessage);
                throw new Exception(errorMessage);
            }
            for (; j < documentBody.ChildEntities.Count; j++)
            {
                if (documentBody.ChildEntities[j].GetType() == typeof(WTable))
                {
                    WTable table = (WTable)documentBody.ChildEntities[j];
                    // Iterates the rows of the table
                    for (int k = 0; k < table.Rows.Count; k++)
                    {
                        // Backup state and parse
                        log.Debug("Backing up and parsing...");
                        caretaker.Backup();
                        bool isCategoryComplete = categoryOriginator.Parse(table, ref k);

                        if (!isCategoryComplete) continue;

                        k--;
                        // Validate the parse and undo if invalid
                        if (!categoryOriginator.Save().IsValid())
                        {
                            log.Debug("Category Parsing invalid, undoing");
                            caretaker.Undo();
                        }
                        else if (categoryOriginator.Save().IsValid())
                        {
                            log.Debug("Category Parsing valid");
                            result.Add((CategoryModel)categoryOriginator.Save());
                            categoryOriginator = new CategoryParser();
                        }
                    }
                }
            }

            if (categoryOriginator.Save().IsValid())
            {
                result.Add((CategoryModel)categoryOriginator.Save());
            }
            else
            {
                CategoryModel x = (CategoryModel)categoryOriginator.Save();

                if (!x.RegulationList.Any()) return result;

                x.RegulationList.RemoveAt(x.RegulationList.Count - 1);
                if (x.IsValid())
                {
                    result.Add(x);
                }
            }
            return result;
        }

        private WordDocument getDocumentFromPath()
        {
            using FileStream fs = File.Open(this.parameterModel.FilePath, FileMode.Open);
            return new WordDocument(fs);
        }

        private int getMainTableIndex(EntityCollection documentBodyEntities)
        {
            // Find the text preceding the main table and return that + 1
            for (int i = 0; i < documentBodyEntities.Count; i++)
            {
                if (documentBodyEntities[i].GetType() == typeof(WParagraph) && ((WParagraph)documentBodyEntities[i]).Text.Contains("Jurisdictional Requirements"))
                {
                    return ++i;
                }
            }
            return -1;
        }
    }
}