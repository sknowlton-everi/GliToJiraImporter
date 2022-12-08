using GliToJiraImporter.Models;
using log4net;
using Syncfusion.DocIO.DLS;
using System.Reflection;
using System.Text.Json;

namespace GliToJiraImporter.Parsers
{
    public class ClauseIdParser : IOriginator
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private ClauseIdModel _state = new ClauseIdModel();

        public ClauseIdParser() { }

        public ClauseIdParser(IMemento state)
        {
            if (state == null)
            {
                this._state = new ClauseIdModel();
            }
            else
            {
                this._state = (ClauseIdModel)state;
            }
            log.Debug("ClauseIdParser: My initial state is: " + JsonSerializer.Serialize(this._state));
        }

        public void Parse(WTableCell cell)
        {
            // Iterates through the paragraphs of the cell
            for (int i = 0; i < cell.Paragraphs.Count; i++)
            {
                WParagraph paragraph = cell.Paragraphs[i];
                if (!paragraph.Text.Equals(string.Empty))
                {
                    if (this._state.BaseClauseId.Equals(string.Empty) && ClauseIdModel.ContainsClauseId(paragraph.Text))
                    {
                        this._state.BaseClauseId = paragraph.Text.Trim();
                        this._state.FullClauseId = paragraph.Text.Trim();
                    }
                    // Check for additions to clauseId
                    else
                    {
                        this._state.FullClauseId += $" --- {paragraph.Text.Trim()}";
                    }
                }
            }
        }

        // Saves the current state inside a memento.
        public IMemento Save()
        {
            return new ClauseIdModel(this._state);
        }

        // Restores the Originator's state from a memento object.
        public void Restore(IMemento memento)
        {
            if (memento is not ClauseIdModel)
            {
                throw new Exception("Unknown memento class " + memento.ToString());
            }

            this._state = (ClauseIdModel)memento.GetState();
            log.Debug($"ClauseIdParser: My state has changed to: {JsonSerializer.Serialize(this._state)}");
        }
    }
}