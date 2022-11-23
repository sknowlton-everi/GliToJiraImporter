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
                }
            }

            if (clauseIdParser.Save().IsValid())
            {
                regulationModel.ClauseID = (ClauseIdModel)clauseIdParser.Save();
            }
            if (descriptionParser.Save().IsValid())
            {
                regulationModel.Description = (DescriptionModel)descriptionParser.Save();
            }

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