using GliToJiraImporter.Models;
using log4net;
using Syncfusion.DocIO.DLS;
using System.Reflection;
using System.Text.Json;

namespace GliToJiraImporter.Parsers
{
    public class CategoryParser : IOriginator
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);

        private CategoryModel _state = new();

        public CategoryParser() { }

        public CategoryParser(CategoryModel state)
        {
            this._state = state;
            log.Debug("CategoryParser: My initial state is: " + JsonSerializer.Serialize(this._state));
        }

        public bool Parse(IWTable table, ref int rowIndex)
        {
            CategoryModel categoryModel = this._state;

            // Originator instantiation
            RegulationParser regulationParser = new RegulationParser();
            if (!categoryModel.IsEmpty() && (categoryModel.RegulationList.Count > 0))
            {
                regulationParser = new RegulationParser((RegulationModel)categoryModel.RegulationList.Last().GetState());
                categoryModel.RegulationList.RemoveAt(categoryModel.RegulationList.Count - 1);
            }
            // Caretaker instantiation
            Caretaker regulationCaretaker = new(regulationParser);

            bool categoryComplete = false;

            // Iterates the rows of the table
            for (; rowIndex < table.Rows.Count && !categoryComplete; rowIndex++)
            {
                // Checks for a gray background, with the idea that they are either a category, sub-category, or the extra header at the start
                WTableRow row = table.Rows[rowIndex];
                if (row.Cells[0].CellFormat.BackColor.Name.Equals("ffd9d9d9")
                    && !row.Cells[0].Paragraphs[0].Text.Equals(string.Empty)
                    && row.Cells.Count > 1)
                {
                    // Add the originators memento if it's valid
                    IMemento regulationParserModel = regulationParser.Save();
                    if (regulationParserModel.IsValid())
                    {
                        categoryModel.RegulationList.Add((RegulationModel)regulationParserModel);
                    }
                    // Clear the originator as it was either invalid or added
                    regulationParser = new RegulationParser(regulationParser.Save());

                    // Checks for a double cell, with the idea that it's a category
                    if (categoryModel.Category.Equals(string.Empty) && row.Cells.Count == 2)
                    {
                        categoryModel.Category = row.Cells[0].Paragraphs[0].Text;
                    }
                    // Checks if a new category is starting
                    else if (row.Cells.Count == 2 || (categoryModel.NoCategory && row.Cells.Count == 3))
                    {
                        rowIndex--;
                        this._state = (CategoryModel)categoryModel.GetState();
                        categoryComplete = true;
                    }
                    else // This row is a subcategory header
                    {
                        RegulationModel newRegulation = new RegulationModel();

                        // Check if a category was found, and if not, sets it using the Subcategory instead
                        if (categoryModel.Category.Equals(string.Empty))
                        {
                            categoryModel.NoCategory = true;
                            categoryModel.Category = row.Cells[0].Paragraphs[0].Text;
                        }
                        else
                        {
                            newRegulation.Subcategory = row.Cells[0].Paragraphs[0].Text;
                        }

                        regulationParser = new RegulationParser(newRegulation);
                    }
                }
                // Continue only if the category has been found, as well as the regulation sub-category unless no category was provided
                else if (!categoryModel.Category.Equals(string.Empty)
                    && (categoryModel.NoCategory || !((RegulationModel)regulationParser.Save()).Subcategory.Equals(string.Empty)))
                {
                    regulationCaretaker.Backup();
                    bool isRegulationComplete = regulationParser.Parse(table.Rows[rowIndex]);
                    if (isRegulationComplete)
                    {
                        rowIndex--;
                        // Validate the parse and undo if invalid
                        if (!regulationParser.Save().IsValid())
                        {
                            log.Error("Regulation Parsing invalid, undoing");
                            regulationCaretaker.Undo();
                        }
                        else if (regulationParser.Save().IsValid())
                        {
                            log.Debug("Regulation Parsing valid");
                            categoryModel.RegulationList.Add((RegulationModel)regulationParser.Save());
                            RegulationModel newRegulationModel = new RegulationModel();
                            newRegulationModel.Subcategory = regulationParser.Save().GetName();
                            regulationParser = new RegulationParser(newRegulationModel);
                        }
                    }
                }
            }

            if (regulationParser.Save().IsValid() && (categoryModel.RegulationList.Count == 0 || !((RegulationModel)regulationParser.Save()).Equals(categoryModel.RegulationList.Last())))
            {
                categoryModel.RegulationList.Add((RegulationModel)regulationParser.Save());
            }
            this._state = (CategoryModel)categoryModel.GetState();
            return categoryComplete;
        }

        // Saves the current state inside a memento.
        public IMemento Save()
        {
            return new CategoryModel(this._state);
        }

        // Restores the Originator's state from a memento object.
        public void Restore(IMemento memento)
        {
            if (!(memento is CategoryModel))
            {
                throw new Exception("Unknown memento class " + memento);
            }

            this._state = (CategoryModel)memento.GetState();
            log.Debug($"CategoryParser: My state has changed to: {JsonSerializer.Serialize(this._state)}");
        }
    }
}