using Aspose.Words;
using Aspose.Words.Tables;
using GliToJiraImporter.Models;
using Syncfusion.DocIO.DLS;
using GliToJiraImporter.Types;
using System.Collections;
using log4net;
using System.Reflection;
using System.Diagnostics;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace GliToJiraImporter
{
    public class Parser
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private ParameterModel parameterModel;

        public Parser(ParameterModel parameterModel)
        {
            log.Info(new StackFrame().GetMethod().Name);
            this.parameterModel = parameterModel;
        }

        public List<CategoryModel> Parse()
        {
            log.Debug("Parsing...");
            List<CategoryModel> result = new List<CategoryModel>();
            if (parameterModel.Type == (int)DocumentType.CHECK_OFF)
            {
                log.Debug("Checkoff Document Type");
                //ParseAspose();
                //ParseSyncFusion();
                result = ParseTable();
            }
            else
            {
                log.Debug("Unknown Document Type");
                //TODO What if it's not a checkoff doc?
            }
            log.Debug($"Completed list size: {result.Count}");
            //log.Debug($"Completed list: {JsonSerializer.Serialize(result)}");
            //logCats(result);
            saveText(@"C:\Users\samantha.knowlton\Documents\Results.txt", JsonSerializer.Serialize(result));
            return result;
        }

        private void ParseAspose()
        {
            //Extract Content Between Different Types of Nodes
            Document docx = new Document(parameterModel.FileName);

            Paragraph startPara = (Paragraph)docx.LastSection.GetChild(NodeType.Paragraph, 2, true);
            Table endTable = (Table)docx.LastSection.GetChild(NodeType.Table, 0, true);

            // Extract the content between these nodes in the document. Include these markers in the extraction.
            //ArrayList extractedNodes = Common.ExtractContent(startPara, endTable, true);

            // Lets reverse the array to make inserting the content back into the document easier.
            //extractedNodes.Reverse();

            //while (extractedNodes.Count > 0)
            //{
            //    // Insert the last node from the reversed list 
            //    endTable.ParentNode.InsertAfter((Node)extractedNodes[0], endTable);
            //    // Remove this node from the list after insertion.
            //    extractedNodes.RemoveAt(0);
            //}
        }

        private void ParseSyncFusion()
        {
            //TODO add logging
            //Opens the Word template document
            using (WordDocument document = new WordDocument(parameterModel.FileName))
            {
                string sCurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                //string p = Path.GetRelativePath(@"C:\MForce\GliToJiraImporter\GliToJiraImporter.Testing", @"C:\MForce\GliToJiraImporter\GliToJiraImporter.Testing\Public\Australia-New-Zealand.docx");
                //string p1 = Path.GetRelativePath(@"C:\MForce\GliToJiraImporter\GliToJiraImporter.Testing", sCurrentDirectory);
                //string p2 = Path.GetRelativePath(sCurrentDirectory, @"C:\MForce\GliToJiraImporter\GliToJiraImporter.Testing\Public\Australia-New-Zealand.docx");
                //Gets the string that contains whole document content as text
                string text = document.GetText();
                //Create a new text file and write specified string in it
                File.WriteAllText(@"C:\MForce\GliToJiraImporter\GliToJiraImporter.Testing\Public\Result.txt", text);

                //document.FindNextSingleLine();
                var t = document.Sections.FirstItem;
                string s = ";";

                /* Parsing steps (if just using the text):
                 * Skip until "Jurisdictional Requirements"
                 * find "NS#.#.#"
                 * copy from there to the next
                 * skip "Choose an item"
                 * check if there is a space above the id, 
                 *      if yes, move on, 
                 *      if no, remove the 3 previous lines
                 */

            }
        }

        private List<CategoryModel> ParseTable()
        {
            List<CategoryModel> result = new List<CategoryModel>();
            List<RegulationModel> regulations = new List<RegulationModel>();

            //Creates an instance of WordDocument class
            WordDocument document = new WordDocument(parameterModel.FileName);
            WSection section = document.Sections[0];
            //WTable table = section.Tables[section.Tables.Count - 1] as WTable;

            //LogTables(section);

            // Skip everything before the actual requirements table in the 'Jurisdictional Requirements' section
            //section = GetStartingPoint(document);//TODO pointless????????????????????

            CategoryModel currentCategory = new CategoryModel();
            RegulationModel currentRegulation = new RegulationModel();

            string currentCategoryName = string.Empty;


            //Iterates the tables of the section
            for (int i = 4; i < section.Tables.Count; i++)
            {
                //Iterates the rows of the table
                foreach (WTableRow row in section.Tables[i].Rows)
                {
                    //TODO Checks for a gray background, with the idea that they are either a category, sub-category, or the extra header at the start
                    string kjlk = row.Cells[0].Paragraphs[0].Text;
                    if (row.Cells[0].CellFormat.BackColor.Name.Equals("ffd9d9d9") && !row.Cells[0].Paragraphs[0].Text.Equals(string.Empty))
                    {
                        // Adds and clears currentRegulation if it isn't empty and is valid
                        if (currentRegulation.IsValid())
                        {
                            currentCategory.RegulationList.Add(currentRegulation);
                        }
                        currentRegulation = new RegulationModel();

                        // Checks for a solo cell, with the idea that it's either a category or the extra header at the start
                        if (row.Cells.Count == 2)
                        {
                            currentCategoryName = row.Cells[0].Paragraphs[0].Text;
                        }
                        else //This row is a subcategory header
                        {
                            // Checks if a new category is starting, then adds and clears the currentCategory if it's valid
                            //if (!currentCategoryName.Equals(string.Empty) && currentCategory.IsValid())
                            if (!currentCategoryName.Equals(string.Empty))
                            {
                                if (currentCategory.IsValid())
                                {
                                    result.Add(currentCategory);
                                    currentCategory = new CategoryModel();
                                }
                                // Stores the category and sub-category names
                                currentCategory.Category = currentCategoryName;
                                currentCategoryName = string.Empty;
                            }
                            currentRegulation.Subcategory = row.Cells[0].Paragraphs[0].Text;
                        }
                    }
                    // Continue only if the category and regulation sub-category have been filled in
                    if (!currentCategory.Category.Equals(string.Empty) && !currentRegulation.Subcategory.Equals(string.Empty))
                    {
                        //Iterates through the cells of rows
                        foreach (WTableCell cell in row.Cells)
                        {
                            //Iterates through the paragraphs of the cell
                            foreach (WParagraph paragraph in cell.Paragraphs)
                            {
                                if (!paragraph.Text.Equals(string.Empty) && !paragraph.Text.Contains("Choose an item"))
                                {
                                    // Check for the clauseId via pattern
                                    Regex clauseIdRegex = new Regex(@"((NS)+(\d)+(.)+(\d)+(.)+(\d))"); //TODO What if the numbers are double digits?
                                    if (currentRegulation.ClauseID.Equals(string.Empty) && clauseIdRegex.IsMatch(paragraph.Text))
                                    {
                                        currentRegulation.ClauseID = paragraph.Text;
                                    }
                                    else if (clauseIdRegex.IsMatch(paragraph.Text))
                                    {
                                        if (currentRegulation.IsValid())
                                        {
                                            currentCategory.RegulationList.Add(currentRegulation);
                                        }
                                        currentRegulation = new RegulationModel();
                                        currentRegulation.ClauseID = paragraph.Text;
                                        currentRegulation.Subcategory = currentCategory.RegulationList.Last().Subcategory;
                                    }
                                    else if (!currentRegulation.ClauseID.Equals(string.Empty))
                                    {
                                        if (!currentRegulation.Description.Equals(string.Empty))
                                        {
                                            currentRegulation.Description += "\n";
                                        }
                                        currentRegulation.Description += paragraph.Text;
                                    }
                                }
                                else if (paragraph.ChildEntities.Count != 0)
                                {
                                    for (int j = 0; j < paragraph.ChildEntities.Count; j++)
                                    {
                                        var ty = paragraph.ChildEntities[j].GetType();
                                        if (paragraph.ChildEntities[j].GetType().Equals(typeof(WPicture)))
                                        {
                                            WPicture picture = (WPicture)paragraph.ChildEntities[j];
                                            currentRegulation.AttachmentList.Add(picture.ImageBytes);

                                        }
                                    }
                                }
                                if (cell.Tables.Count != 0 && paragraph.Text.Equals(string.Empty))
                                {
                                    foreach (WTable subTable in cell.Tables)
                                    {
                                        string tableInfo = "||";
                                        foreach (WTableCell subCell in subTable.Rows[0].Cells)
                                        {
                                            tableInfo += $"{subCell.Paragraphs[0].Text}||";
                                        }
                                        for (int k = 1; k < subTable.Rows.Count; k++)
                                        {
                                            tableInfo += "\n";
                                            foreach (WTableCell subCell in subTable.Rows[k].Cells)
                                            {
                                                foreach (WParagraph subParagraph in subCell.Paragraphs)
                                                {
                                                    tableInfo += $"|{subParagraph.Text} ";
                                                }
                                                tableInfo = tableInfo.Trim();
                                            }
                                            tableInfo += "|";
                                        }
                                        if (!currentRegulation.Description.Equals(string.Empty))
                                        {
                                            currentRegulation.Description += "\n";
                                        }
                                        currentRegulation.Description += $"{tableInfo}";
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //Saves and closes the document instance
            //document.Save("Sample.docx", FormatType.Docx);
            document.Close();
            if (currentRegulation.IsValid())
            {
                currentCategory.RegulationList.Add(currentRegulation);
            }
            if (currentCategory.IsValid())
            {
                result.Add(currentCategory);
            }

            return result;
        }

        private WSection GetStartingPoint(WordDocument document)
        {
            StringBuilder sb = new StringBuilder();
            WSection result = null;
            for (int i = 0; i < document.Sections.Count; i++)
            {
                WSection section = document.Sections[i];
                sb.AppendLine($"Section {i}: ");//{JsonSerializer.Serialize(result)}");
                                                //foreach (WParagraph paragraph in result.Paragraphs)
                                                //{
                for (int j = 0; j < section.Paragraphs.Count; j++)
                {
                    //log.Debug($"Section {i} - Paragraph {j}/{result.Paragraphs.Count}: {JsonSerializer.Serialize(result.Paragraphs[j])}");
                    sb.AppendLine($"Section {i} - Paragraph {j}/{section.Paragraphs.Count}: {section.Paragraphs[j].Text}");
                    if (section.Paragraphs[j].Text.Contains("Jurisdictional Requirements"))
                    {
                        sb.AppendLine($"Next Paragraph {j + 1}: {section.Paragraphs[j + 1].Text}");
                        sb.AppendLine($"Next Section {i + 1}: ");//{JsonSerializer.Serialize(document.Sections[i+1])}");
                        result = section;
                        break;
                    }
                    if (result != null)
                    {
                        break;
                    }
                }
                //}
            }
            if (result == null)
            {
                log.Error("Failed to find starting point.");
            }
            log.Debug($"GetStartingPoint: \n{sb.ToString()}");
            return result;
        }

        private void LogTables(WSection section)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < section.Tables.Count; i++)
            {
                string s = $"Table {i}: ";
                //sb.AppendLine($"Table {i}");
                //Iterates the rows of the table
                for (int j = 0; j < section.Tables[i].Rows.Count; j++)
                {
                    s += $", Row {j}: ";
                    //sb.AppendLine($"Row {j}");
                    //Iterates through the cells of rows
                    for (int k = 0; k < section.Tables[i].Rows[j].Cells.Count; k++)
                    {
                        s += $", Cell {k}: ";
                        //sb.AppendLine($"Cell {k}");
                        //Iterates through the paragraphs of the cell
                        for (int l = 0; l < section.Tables[i].Rows[j].Cells[k].Paragraphs.Count; l++)
                        {
                            sb.AppendLine($"{s}, Paragraph {l}: {section.Tables[i].Rows[j].Cells[k].Paragraphs[l].Text}");
                        }
                    }
                }
            }
            saveText(@"C:\Users\samantha.knowlton\Documents\Tables.txt", sb.ToString());
            log.Debug($"Tables: \n{sb.ToString()}");
            ////Saves and closes the document instance
            //document.Save("Sample.docx", FormatType.Docx);
            //document.Close();
        }
        private void logCats(List<CategoryModel> c)
        {
            StringBuilder sb = new StringBuilder();
            foreach (CategoryModel cat in c)
            {
                sb.AppendLine($"{cat.Category}: Regulations - {cat.RegulationList.Count} ");
                string subcat = string.Empty;
                int inSubCat = 1;
                foreach (RegulationModel reg in cat.RegulationList)
                {
                    if (subcat.Equals(string.Empty))
                    {
                        subcat = reg.Subcategory;
                        sb.Append($"{reg.Subcategory}");
                    }
                    else
                    {
                        if (reg.Subcategory.Equals(subcat))
                        {
                            inSubCat++;
                        }
                        else
                        {
                            sb.AppendLine($": {inSubCat}");
                            inSubCat = 1;
                            sb.Append($"{reg.Subcategory}");
                            subcat = reg.Subcategory;
                        }
                    }
                }
                //sb.AppendLine($": {inSubCat}");
                sb.AppendLine("\n----------------------------------------");
            }
            saveText(@"C:\Users\samantha.knowlton\Documents\catLog.txt", sb.ToString());
        }

        private void saveText(string fileName, string text)
        {
            try
            {
                // Check if file already exists. If yes, delete it.     
                if (File.Exists(fileName)) File.Delete(fileName);

                // Create a new file     
                using (FileStream fs = File.Create(fileName))
                {
                    // Add some text to file    
                    Byte[] textBytes = new UTF8Encoding(true).GetBytes(text);
                    fs.Write(textBytes, 0, textBytes.Length);
                }
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex.ToString());
            }
        }
    }
}
