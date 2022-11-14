using GliToJiraImporter.Models;
using log4net;
using Syncfusion.DocIO.DLS;
using System.Reflection;
using System.Text.Json;

namespace GliToJiraImporter.Parsers
{
    public class EmbeddedTableParser : IOriginator
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private EmbeddedTableModel _state = new EmbeddedTableModel();

        public EmbeddedTableParser() { }

        public EmbeddedTableParser(EmbeddedTableModel state)
        {
            if (state == null)
            {
                this._state = new EmbeddedTableModel();
            }
            else
            {
                this._state = state;
            }
            log.Debug("EmbeddedTableParser: My initial state is: " + JsonSerializer.Serialize(this._state));
        }

        public void Parse(WTableCell cell)
        {
            foreach (WTable subTable in cell.Tables)
            {
                string tableInfo = "||";
                foreach (WTableCell subCell in subTable.Rows[0].Cells)
                {
                    tableInfo += $"{subCell.Paragraphs[0].Text}||";
                    this._state.Headers.Add(subCell.Paragraphs[0].Text);
                }
                for (int k = 1; k < subTable.Rows.Count; k++)
                {
                    tableInfo += "\n";
                    this._state.ValueRows.Add("");
                    foreach (WTableCell subCell in subTable.Rows[k].Cells)
                    {
                        foreach (WParagraph subParagraph in subCell.Paragraphs)
                        {
                            this._state.ValueRows[k - 1] += $"|{subParagraph.Text} ";
                        }
                        this._state.ValueRows[k - 1] = this._state.ValueRows[k - 1].TrimEnd();
                    }
                    this._state.ValueRows[k - 1] += "|";
                    tableInfo += this._state.ValueRows[k - 1];
                }
                if (!this._state.FormattedTable.Equals(string.Empty))
                {
                    this._state.FormattedTable += "\n";
                }
                this._state.FormattedTable += $"{tableInfo}";
            }
        }

        // Saves the current state inside a memento.
        public IMemento Save()
        {
            return new EmbeddedTableModel(this._state);
        }

        // Restores the Originator's state from a memento object.
        public void Restore(IMemento memento)
        {
            if (!(memento is EmbeddedTableModel))
            {
                throw new Exception("Unknown memento class " + memento.ToString());
            }

            this._state = (EmbeddedTableModel)memento.GetState();
            log.Debug($"EmbeddedTableParser: My state has changed to: {JsonSerializer.Serialize(this._state)}");
        }
    }
}