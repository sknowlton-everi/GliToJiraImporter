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
    public class EmbeddedTableParser : IOriginator
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private RegulationExtrasModel _state = new RegulationExtrasModel();

        public EmbeddedTableParser() { }

        public EmbeddedTableParser(RegulationExtrasModel state)
        {
            this._state = state;
            log.Debug("EmbeddedTableParser: My initial state is: " + JsonSerializer.Serialize(this._state));
            if (this._state == null)
            {
                this._state = new RegulationExtrasModel();
            }
        }
        public void Parse(WTableCell cell)
        {
            foreach (WTable subTable in cell.Tables)
            {
                string tableInfo = "\n||";
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
                        tableInfo = tableInfo.TrimEnd();
                    }
                    tableInfo += "|";
                }
                if (!this._state.State.Equals(string.Empty))
                {
                    this._state.State += "\n";
                }
                this._state.State += $"{tableInfo}";
            }
        }

        // Saves the current state inside a memento.
        public IMemento Save()
        {
            return this._state;
        }

        // Restores the Originator's state from a memento object.
        public void Restore(IMemento memento)
        {
            if (!(memento is RegulationExtrasModel))
            {
                throw new Exception("Unknown memento class " + memento.ToString());
            }

            this._state = (RegulationExtrasModel)memento.GetState();
            log.Debug($"EmbeddedTableParser: My state has changed to: {JsonSerializer.Serialize(this._state)}");
        }
    }
}
