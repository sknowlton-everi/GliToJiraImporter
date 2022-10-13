using GliToJiraImporter.Models;
using log4net;
using Syncfusion.DocIO.DLS;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace GliToJiraImporter.Parsers
{
    public class RegulationParser : IOriginator
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private RegulationModel _state;

        public RegulationParser()
        {
            _state = new RegulationModel();
        }

        public RegulationParser(RegulationModel state)
        {
            this._state = state;
            log.Debug("RegulationParser: My initial state is: " + JsonSerializer.Serialize(this._state));
            if (this._state == null)
            {
                this._state = new RegulationModel();
            }
        }

        public bool Parse(WTableRow row)
        {
            RegulationModel regulationModel = this._state;

            ClauseIdParser clauseIdParser = new ClauseIdParser();
            Caretaker clauseIdCaretaker = new Caretaker(clauseIdParser);
            DescriptionParser descriptionParser = new DescriptionParser(new RegulationExtrasModel(regulationModel.Description));
            Caretaker descriptionCaretaker = new Caretaker(descriptionParser);
            PictureParser pictureParser = new PictureParser();
            Caretaker pictureCaretaker = new Caretaker(pictureParser);
            EmbeddedTableParser embeddedTableParser = new EmbeddedTableParser();
            Caretaker embeddedTableCaretaker = new Caretaker(embeddedTableParser);

            bool regulationComplete = false;

            // Iterates through the cells of rows
            for (int i = 0; i < row.Cells.Count && !regulationComplete; i++)
            {
                // Iterates through the paragraphs of the cell
                for (int j = 0; j < row.Cells[i].Paragraphs.Count && !regulationComplete; j++)
                {
                    WParagraph paragraph = row.Cells[i].Paragraphs[j];
                    // Checks for ClauseId or description within the cell
                    if (!paragraph.Text.Equals(string.Empty) && !paragraph.Text.Contains("Choose an item"))
                    {
                        clauseIdParser.Parse(paragraph);
                        // If a clauseId was parsed and that the current models clauseId is empty, then save it if so
                        if (!clauseIdParser.Save().GetState().Equals(string.Empty) && regulationModel.ClauseID.Equals(string.Empty))
                        {
                            regulationModel.ClauseID = clauseIdParser.Save().GetName();
                            clauseIdParser = new ClauseIdParser();
                        }
                        // If a clauseId was parsed, but the current model already has a clauseId, then a new regulation has been found
                        else if (!clauseIdParser.Save().GetName().Equals(string.Empty))
                        {
                            this._state = (RegulationModel)regulationModel.GetState();
                            regulationComplete = true;
                            break;
                        }
                        // Verify it's not ClauseID to ensure it's description
                        else if (!regulationModel.ClauseID.Equals(string.Empty))
                        {
                            descriptionCaretaker.Backup();
                            descriptionParser.Parse(paragraph);
                            if (!descriptionParser.Save().IsValid())
                            {
                                descriptionCaretaker.Undo();
                            }
                            else
                            {
                                regulationModel.Description = descriptionParser.Save().GetName();
                            }
                        }
                    }
                    // Checks for a picture within a cell 
                    else if (paragraph.ChildEntities.Count != 0)
                    {
                        pictureCaretaker.Backup();
                        pictureParser.Parse(paragraph);
                        if (!pictureParser.Save().IsValid())
                        {
                            pictureCaretaker.Undo();
                        }
                        else
                        {
                            regulationModel.AttachmentList.Add((PictureModel)pictureParser.Save());
                        }
                    }
                    // Checks for a table within a cell 
                    if (row.Cells[i].Tables.Count != 0 && paragraph.Text.Equals(string.Empty))
                    {
                        embeddedTableCaretaker.Backup();
                        embeddedTableParser.Parse(row.Cells[i]);
                        if (!embeddedTableParser.Save().IsValid())
                        {
                            embeddedTableCaretaker.Undo();
                        }
                        else
                        {
                            // Add the embedded table to the end of the description
                            descriptionParser.Restore(new RegulationExtrasModel(descriptionParser.Save().GetName() + embeddedTableParser.Save().GetName()));
                        }
                    }
                }
            }

            if (!regulationComplete && ((RegulationExtrasModel)clauseIdParser.Save()).IsValidClauseId())
            {
                regulationModel.ClauseID = clauseIdParser.Save().GetName();
            }
            if (!regulationComplete && ((RegulationExtrasModel)descriptionParser.Save()).IsValidDescription())
            {
                regulationModel.Description = descriptionParser.Save().GetName();
            }
            this._state = (RegulationModel)regulationModel.GetState();
            return regulationComplete;
        }

        // Saves the current state inside a memento.
        public IMemento Save()
        {
            return this._state;
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
