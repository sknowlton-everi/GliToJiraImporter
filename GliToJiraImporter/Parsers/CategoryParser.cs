using GliToJiraImporter.Models;
using log4net;
using Syncfusion.DocIO.DLS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GliToJiraImporter.Parsers
{
    public class CategoryParser : IOriginator
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private CategoryModel _state;

        public CategoryParser() 
        {
            _state = new CategoryModel();
        }

        public CategoryParser(CategoryModel state)
        {
            this._state = state;
            log.Debug("CategoryParser: My initial state is: " + JsonSerializer.Serialize(this._state));
            if (this._state == null)
            {
                this._state = new CategoryModel();
            }
        }

        public bool Parse(IWTable table, ref int rowIndex)
        {
            CategoryModel categoryModel = this._state;
            if (categoryModel == null)
            {
                categoryModel = new CategoryModel();
            }
            string currentCategoryName = string.Empty;

            // Originator instantiation
            RegulationParser regulationOriginator = new RegulationParser();
            if (!categoryModel.IsEmpty() && (categoryModel.RegulationList.Count > 0))
            {
                regulationOriginator = new RegulationParser((RegulationModel)categoryModel.RegulationList.Last().GetState());
                categoryModel.RegulationList.RemoveAt(categoryModel.RegulationList.Count - 1);
            }
            // Caretaker instantiation
            Caretaker caretaker = new Caretaker(regulationOriginator);

            bool categoryComplete = false;

            // Iterates the rows of the table
            for (; rowIndex < table.Rows.Count && !categoryComplete; rowIndex++)
            {
                Console.WriteLine(rowIndex);
                // Checks for a gray background, with the idea that they are either a category, sub-category, or the extra header at the start
                WTableRow x = table.Rows[rowIndex];
                if (x.Cells[0].CellFormat.BackColor.Name.Equals("ffd9d9d9") && !x.Cells[0].Paragraphs[0].Text.Equals(string.Empty))
                {
                    // Add the originators memento if it's valid
                    IMemento y = regulationOriginator.Save();
                    if (y != null && y.IsValid())
                    {
                        categoryModel.RegulationList.Add((RegulationModel)y);
                    }
                    // Clear the originator as it was either invalid or added
                    regulationOriginator = new RegulationParser((RegulationModel)regulationOriginator.Save());

                    // Checks for a double cell, with the idea that it's a category
                    if (categoryModel.Category.Equals(string.Empty) && x.Cells.Count == 2)
                    {
                        categoryModel.Category = x.Cells[0].Paragraphs[0].Text;
                    }
                    // Checks if a new category is starting, and exits if so
                    else if (x.Cells.Count == 2)
                    {
                        rowIndex--;
                        this._state = (CategoryModel)categoryModel.GetState();
                        categoryComplete = true;
                    }
                    else //This row is a subcategory header
                    {
                        RegulationModel z = new RegulationModel();
                        z.Subcategory = x.Cells[0].Paragraphs[0].Text;
                        regulationOriginator = new RegulationParser(z);
                    }
                }
                // Continue only if the category and regulation sub-category have been filled in
                else if (!categoryModel.Category.Equals(string.Empty) && !((RegulationModel)regulationOriginator.Save()).Subcategory.Equals(string.Empty))
                {
                    caretaker.Backup();
                    bool isRegulationComplete = regulationOriginator.Parse(table.Rows[rowIndex]);
                    if (isRegulationComplete)
                    {
                        rowIndex--;
                        // Validate the parse and undo if invalid
                        if (!regulationOriginator.Save().IsValid())
                        {
                            log.Error("Regulation Parsing invalid, undoing");
                            caretaker.Undo();
                        }
                        else if (regulationOriginator.Save().IsValid())
                        {
                            log.Debug("Regulation Parsing valid");
                            categoryModel.RegulationList.Add((RegulationModel)regulationOriginator.Save());
                            RegulationModel newRegulationModel = new RegulationModel();
                            newRegulationModel.Subcategory = regulationOriginator.Save().GetName();
                            regulationOriginator = new RegulationParser(newRegulationModel);
                        }
                    }
                }

            }

            if (regulationOriginator.Save().IsValid() && (categoryModel.RegulationList.Count == 0 || !((RegulationModel)regulationOriginator.Save()).ClauseID.Equals(categoryModel.RegulationList.Last().ClauseID)))
            {
                categoryModel.RegulationList.Add((RegulationModel)regulationOriginator.Save());
            }
            this._state = (CategoryModel)categoryModel.GetState();
            return categoryComplete;
        }


        // Saves the current state inside a memento.
        public IMemento Save()
        {
            return this._state;
        }

        // Restores the Originator's state from a memento object.
        public void Restore(IMemento memento)
        {
            if (!(memento is CategoryModel))
            {
                throw new Exception("Unknown memento class " + memento.ToString());
            }

            this._state = (CategoryModel)memento.GetState();
            log.Debug($"CategoryParser: My state has changed to: {JsonSerializer.Serialize(this._state)}");
        }
    }
}
