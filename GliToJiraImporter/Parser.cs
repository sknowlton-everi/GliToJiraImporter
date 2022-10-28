using GliToJiraImporter.Models;
using Syncfusion.DocIO.DLS;
using GliToJiraImporter.Types;
using log4net;
using System.Reflection;
using System.Diagnostics;
using System.Text.RegularExpressions;
using GliToJiraImporter.Parsers;
using Atlassian.Jira;
using GliToJiraImporter.Utilities;
using System.Text.Json;

namespace GliToJiraImporter
{
    public class Parser
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private ParameterModel parameterModel;
        private StorageUtilities storageUtilities;

        public Parser(ParameterModel parameterModel, Jira jiraConnection)
        {
            log.Info(new StackFrame().GetMethod().Name);
            this.parameterModel = parameterModel;
            this.storageUtilities = new StorageUtilities(this.parameterModel, jiraConnection);
        }

        public IList<CategoryModel> Parse()
        {
            log.Debug("Checking doc type...");
            IList<CategoryModel> result;
            if (parameterModel.Type == (int)DocumentType.CHECK_OFF)
            {
                log.Debug($"Document Type: {parameterModel.GetType().Name}");
                log.Debug("Parsing...");
                result = this.parseMementos();
            }
            else
            {
                log.Debug($"Document Type Unknown: {parameterModel.Type}");
                // Log and throw unknown type
                throw new Exception("Provided Document Type is Unknown. Try type 1 for Checkoff documents.");//TODO is this okay? I don't like the vagueness of type 'Exception'
            }
            log.Debug($"Completed list size: {result.Count}");
            storageUtilities.UploadToJira(result);
            // Uncomment if you want to save results to the public folder in the test project
            //this.storageUtilities.SaveText(@"..\..\..\..\GliToJiraImporter.Testing\Public\Results.txt", JsonSerializer.Serialize(result));
            //this.storageUtilities.SaveCsv(@"..\..\..\..\GliToJiraImporter.Testing\Public\ResultsCsv.csv", result);
            return result;
        }

        private IList<CategoryModel> parseMementos()
        {
            IList<CategoryModel> result = new List<CategoryModel>();

            // Originator and Caretaker instantiation
            CategoryParser categoryoriginator = new CategoryParser();
            Caretaker caretaker = new Caretaker(categoryoriginator);

            // Creates an instance of WordDocument class
            WSection section = new WordDocument(parameterModel.FilePath).Sections[0];

            // Iterates the tables of the section
            //TODO if i is set to anything below 4, the tests get the following error. When running it myself it seems to maybe be an infinite loop issue
            //  log4net:ERROR RollingFileAppender: INTERNAL ERROR. Append is False but OutputFile [C:\MForce\GliToJiraImporter\GliToJiraImporter.Testing\bin\Debug\net6.0\gliToJiraImporter.log] already exists.
            for (int i = 4; i < section.Tables.Count; i++)
            {
                // Iterates the rows of the table
                for (int j = 0; j < section.Tables[i].Rows.Count; j++)
                {
                    // Backup state and parse
                    log.Debug("Backing up and parsing...");
                    caretaker.Backup();
                    bool isCategoryComplete = categoryoriginator.Parse(section.Tables[i], ref j);

                    if (isCategoryComplete)
                    {
                        j--;
                        // Validate the parse and undo if invalid
                        if (!categoryoriginator.Save().IsValid())
                        {
                            log.Debug("Category Parsing invalid, undoing");
                            caretaker.Undo();
                        }
                        else if (categoryoriginator.Save().IsValid())
                        {
                            log.Debug("Category Parsing valid");
                            result.Add((CategoryModel)categoryoriginator.Save());
                            categoryoriginator = new CategoryParser();
                        }
                    }
                }
            }

            if (categoryoriginator.Save().IsValid())
            {
                result.Add((CategoryModel) categoryoriginator.Save());
            }
            else
            {
                CategoryModel x = (CategoryModel)categoryoriginator.Save();
                if (x.RegulationList.Any())
                {
                    x.RegulationList.RemoveAt(x.RegulationList.Count - 1);
                    if (x.IsValid())
                    {
                        result.Add(x);
                    }
                }
            }
            return result;
        }


        // Ignore. This is the previous way of parsing 
        //private IList<CategoryModel> parseTable()
        //{
        //    IList<CategoryModel> result = new List<CategoryModel>();

        //    //Creates an instance of WordDocument class
        //    WSection section = new WordDocument(parameterModel.FilePath).Sections[0];

        //    CategoryModel currentCategory = new CategoryModel();
        //    RegulationModel currentRegulation = new RegulationModel();
        //    string currentCategoryName = string.Empty;

        //    // Iterates the tables of the section
        //    for (int i = 0; i < section.Tables.Count; i++)
        //    {
        //        // Iterates the rows of the table
        //        foreach (WTableRow row in section.Tables[i].Rows)
        //        {
        //            // Checks for a gray background, with the idea that they are either a category, sub-category, or the extra header at the start
        //            if (row.Cells[0].CellFormat.BackColor.Name.Equals("ffd9d9d9") && !row.Cells[0].Paragraphs[0].Text.Equals(string.Empty))
        //            {
        //                // Adds and clears currentRegulation if it isn't empty and is valid
        //                if (currentRegulation.IsValid())
        //                {
        //                    currentCategory.RegulationList.Add(currentRegulation);
        //                }
        //                currentRegulation = new RegulationModel();

        //                // Checks for a solo cell, with the idea that it's either a category or the extra header at the start
        //                if (row.Cells.Count == 2)
        //                {
        //                    currentCategoryName = row.Cells[0].Paragraphs[0].Text;
        //                }
        //                else // This row is a subcategory header
        //                {
        //                    // Checks if a new category is starting, then adds and clears the currentCategory if it's valid
        //                    if (!currentCategoryName.Equals(string.Empty))
        //                    {
        //                        if (currentCategory.IsValid())
        //                        {
        //                            result.Add(currentCategory);
        //                            currentCategory = new CategoryModel(); //TODO Figure out why I put a todo here
        //                        }
        //                        // Stores the category and sub-category names
        //                        currentCategory.Category = currentCategoryName;
        //                        currentCategoryName = string.Empty;
        //                    }
        //                    currentRegulation.Subcategory = row.Cells[0].Paragraphs[0].Text;
        //                }
        //            }
        //            // Continue only if the category and regulation sub-category have been filled in
        //            if (!currentCategory.Category.Equals(string.Empty) && !currentRegulation.Subcategory.Equals(string.Empty))
        //            {
        //                // Iterates through the cells of rows
        //                foreach (WTableCell cell in row.Cells)
        //                {
        //                    // Iterates through the paragraphs of the cell
        //                    foreach (WParagraph paragraph in cell.Paragraphs)
        //                    {
        //                        if (!paragraph.Text.Equals(string.Empty) && !paragraph.Text.Contains("Choose an item"))
        //                        {
        //                            // Check for the clauseId via pattern
        //                            Regex clauseIdRegex = new Regex(@"((NS)+(\d)+(.)+(\d)+(.)+(\d))");
        //                            if (currentRegulation.ClauseID.Equals(string.Empty) && clauseIdRegex.IsMatch(paragraph.Text))
        //                            {
        //                                currentRegulation.ClauseID = paragraph.Text;
        //                            }
        //                            else if (clauseIdRegex.IsMatch(paragraph.Text))
        //                            {
        //                                if (currentRegulation.IsValid())
        //                                {
        //                                    currentCategory.RegulationList.Add(currentRegulation);
        //                                }
        //                                currentRegulation = new RegulationModel();
        //                                currentRegulation.ClauseID = paragraph.Text;
        //                                currentRegulation.Subcategory = ((RegulationModel)currentCategory.RegulationList.Last()).Subcategory;
        //                            }
        //                            // Check for ClauseID to verify description
        //                            else if (!currentRegulation.ClauseID.Equals(string.Empty))
        //                            {
        //                                if (!currentRegulation.Description.Equals(string.Empty))
        //                                {
        //                                    currentRegulation.Description += "\n";
        //                                }
        //                                currentRegulation.Description += paragraph.Text;
        //                            }
        //                        }
        //                        // Check for a picture within a table 
        //                        else if (paragraph.ChildEntities.Count != 0)
        //                        {
        //                            for (int j = 0; j < paragraph.ChildEntities.Count; j++)
        //                            {
        //                                Type type = paragraph.ChildEntities[j].GetType();
        //                                if (paragraph.ChildEntities[j].GetType().Equals(typeof(WPicture)))
        //                                {
        //                                    WPicture picture = (WPicture)paragraph.ChildEntities[j];
        //                                    currentRegulation.AttachmentList.Add(picture.ImageBytes);
        //                                }
        //                            }
        //                        }
        //                        // Check for a table within a table 
        //                        if (cell.Tables.Count != 0 && paragraph.Text.Equals(string.Empty))
        //                        {
        //                            foreach (WTable subTable in cell.Tables)
        //                            {
        //                                string tableInfo = "||";
        //                                foreach (WTableCell subCell in subTable.Rows[0].Cells)
        //                                {
        //                                    tableInfo += $"{subCell.Paragraphs[0].Text}||";
        //                                }
        //                                for (int k = 1; k < subTable.Rows.Count; k++)
        //                                {
        //                                    tableInfo += "\n";
        //                                    foreach (WTableCell subCell in subTable.Rows[k].Cells)
        //                                    {
        //                                        foreach (WParagraph subParagraph in subCell.Paragraphs)
        //                                        {
        //                                            tableInfo += $"|{subParagraph.Text} ";
        //                                        }
        //                                        tableInfo = tableInfo.Trim();
        //                                    }
        //                                    tableInfo += "|";
        //                                }
        //                                if (!currentRegulation.Description.Equals(string.Empty))
        //                                {
        //                                    currentRegulation.Description += "\n";
        //                                }
        //                                currentRegulation.Description += $"{tableInfo}";
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    // Add any extras missed at the end
        //    if (currentRegulation.IsValid())
        //    {
        //        currentCategory.RegulationList.Add(currentRegulation);
        //    }
        //    if (currentCategory.IsValid())
        //    {
        //        result.Add(currentCategory);
        //    }

        //    return result;
        //}
    }
}
