using GliToJiraImporter.Models;
using log4net;
using Syncfusion.DocIO.DLS;
using System.Reflection;
using System.Text.Json;

namespace GliToJiraImporter.Parsers
{
    public class RegulationParser : IOriginator
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private RegulationModel _state = new RegulationModel();

        public RegulationParser() { }

        public RegulationParser(IMemento state)
        {
            if (state == null)
            {
                this._state = new RegulationModel();
            }
            else
            {
                this._state = (RegulationModel)state;
            }
            log.Debug("RegulationParser: My initial state is: " + JsonSerializer.Serialize(this._state));
        }

        public bool Parse(WTableRow row)
        {
            RegulationModel regulationModel = this._state;

            ClauseIdParser clauseIdParser = new ClauseIdParser((ClauseIdModel)regulationModel.ClauseID);
            Caretaker clauseIdCaretaker = new Caretaker(clauseIdParser);
            DescriptionParser descriptionParser = new DescriptionParser(regulationModel.Description);
            Caretaker descriptionCaretaker = new Caretaker(descriptionParser);
            //PictureParser pictureParser = new PictureParser();  //TODO Cleanup
            //Caretaker pictureCaretaker = new Caretaker(pictureParser);
            //EmbeddedTableParser embeddedTableParser = new EmbeddedTableParser();
            //Caretaker embeddedTableCaretaker = new Caretaker(embeddedTableParser);

            bool regulationComplete = false;

            // Iterates through the cells of rows
            for (int i = 0; i < row.Cells.Count && !regulationComplete; i++)
            {
                // Checks for the first cell in a row, assuming it's clauseId
                if (i == 0 && !row.Cells[i].Paragraphs[0].Text.Equals(string.Empty))
                {
                    clauseIdCaretaker.Backup();
                    bool clauseIdExists = clauseIdParser.Save().IsValid();
                    clauseIdParser.Parse(row.Cells[i]);
                    if (!clauseIdParser.Save().IsValid())
                    {
                        clauseIdCaretaker.Undo();
                    }
                    // If a clauseId was parsed, but there was a previously parsed one, then a new regulation has been found
                    else if (clauseIdExists && clauseIdParser.Save().IsValid())
                    {
                        clauseIdCaretaker.Undo();
                        regulationComplete = true;
                        break;
                    }
                    //// If a clauseId was parsed and there wasn't a previous one, then save it //TODO Cleanup
                    //else
                    //{
                    //    regulationModel.ClauseID = clauseIdParser.Save().GetName();
                    //}
                }
                // Anything after is assumed to be description
                else if (clauseIdParser.Save().IsValid())
                {
                    descriptionCaretaker.Backup();
                    descriptionParser.Parse(row.Cells[i]);
                    if (!descriptionParser.Save().IsValid())
                    {
                        descriptionCaretaker.Undo();
                    }

                    //// Checks for a picture within a cell  //TODO Cleanup
                    //if (descriptionParser.Save().GetName().Contains("(# Potential image captured #)"))
                    //{
                    //    pictureCaretaker.Backup();
                    //    pictureParser.Parse(paragraph);
                    //    if (!pictureParser.Save().IsValid())
                    //    {
                    //        pictureCaretaker.Undo();
                    //    }
                    //    else
                    //    {
                    //        regulationModel.AttachmentList.Add((PictureModel)pictureParser.Save());
                    //    }
                    //}
                }

                //TODO Cleanup
                //// Iterates through the paragraphs of the cell
                //for (int j = 0; j < row.Cells[i].Paragraphs.Count && !regulationComplete; j++)
                //{
                //    WParagraph paragraph = row.Cells[i].Paragraphs[j];
                //// Checks for ClauseId or description within the cell
                //if (!paragraph.Text.Equals(string.Empty) && !paragraph.Text.Contains("Choose an item"))
                //{
                //if (i == 0)
                //{
                //    clauseIdParser.Parse(paragraph);
                //}

                //TODO Cleanup
                //// If a clauseId was parsed and the current models clauseId is empty, then save it
                //if (!clauseIdParser.Save().GetState().Equals(string.Empty) && regulationModel.ClauseID.Equals(string.Empty))
                //{
                //    regulationModel.ClauseID = clauseIdParser.Save().GetName();
                //    clauseIdParser = new ClauseIdParser();
                //}
                //// If a clauseId was parsed, but the current model already has a clauseId, then a new regulation has been found
                //else if (!clauseIdParser.Save().GetName().Equals(string.Empty))
                //{
                //    this._state = (RegulationModel)regulationModel.GetState();
                //    regulationComplete = true;
                //    break;
                //}
                //    // Verify it's not ClauseID to ensure it's description
                //else if (!regulationModel.ClauseID.Equals(string.Empty))
                //{
                //    descriptionCaretaker.Backup();
                //    descriptionParser.Parse(paragraph);
                //    if (!descriptionParser.Save().IsValid())
                //    {
                //        descriptionCaretaker.Undo();
                //    }
                //    else
                //    {
                //        regulationModel.Description = descriptionParser.Save().GetName();
                //    }
                //}
                //}
                //// Checks for a picture within a cell 
                //else if (paragraph.ChildEntities.Count != 0)
                //{
                //    pictureCaretaker.Backup();
                //    pictureParser.Parse(paragraph);
                //    if (!pictureParser.Save().IsValid())
                //    {
                //        pictureCaretaker.Undo();
                //    }
                //    else
                //    {
                //        regulationModel.AttachmentList.Add((PictureModel)pictureParser.Save());
                //    }
                //}
                //// Checks for a table within a cell 
                //if (row.Cells[i].Tables.Count != 0 && paragraph.Text.Equals(string.Empty))
                //{
                //    embeddedTableCaretaker.Backup();
                //    embeddedTableParser.Parse(row.Cells[i]);
                //    if (!embeddedTableParser.Save().IsValid())
                //    {
                //        embeddedTableCaretaker.Undo();
                //    }
                //    else
                //    {
                //        // Add the embedded table to the end of the description
                //        descriptionParser.Restore(new RegulationExtrasModel(descriptionParser.Save().GetName() + embeddedTableParser.Save().GetName()));
                //    }
                //}
                //}
            }

            if (clauseIdParser.Save().IsValid())
            {
                regulationModel.ClauseID = (ClauseIdModel)clauseIdParser.Save();
            }
            if (descriptionParser.Save().IsValid())
            {
                regulationModel.Description = (DescriptionModel)descriptionParser.Save();
            }

            //TODO Cleanup
            //if (!regulationComplete && ((RegulationExtrasModel)clauseIdParser.Save()).IsValidClauseId())
            //{
            //    regulationModel.ClauseID = clauseIdParser.Save().GetName();
            //}
            //if (!regulationComplete && ((RegulationExtrasModel)descriptionParser.Save()).IsValidDescription())
            //{
            //    regulationModel.Description = descriptionParser.Save().GetName();
            //}
            this._state = (RegulationModel)regulationModel.GetState();
            return regulationComplete;
        }

        // Saves the current state inside a memento.
        public IMemento Save()
        {
            return new RegulationModel(this._state);
        }

        // Restores the Originator's state from a memento object.
        public void Restore(IMemento memento)
        {
            if (!(memento is RegulationModel))
            {
                throw new Exception("Unknown memento class " + memento.ToString());
            }

            this._state = (RegulationModel)memento.GetState();
            log.Debug($"RegulationParser: My state has changed to: {JsonSerializer.Serialize(this._state)}");
        }
    }
}