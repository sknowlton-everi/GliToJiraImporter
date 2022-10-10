using GliToJiraImporter.Models;
using log4net;
using Syncfusion.DocIO.DLS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GliToJiraImporter.Parsers
{
    public class EmbeddedTableParser// : IOriginator
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private string _state = string.Empty;

        public EmbeddedTableParser() { }

        public EmbeddedTableParser(string state)
        {
            this._state = state;
            log.Debug("EmbeddedTableParser: My initial state is: " + state);
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
                if (!this._state.Equals(string.Empty))
                {
                    this._state += "\n";
                }
                this._state += $"{tableInfo}";
            }
        }

        // Saves the current state inside a memento.
        public IMemento Save()
        {
            return new RegulationExtrasModel(this._state);
        }

        //// Restores the Originator's state from a memento object.
        //public void Restore(IMemento memento)
        //{
        //    if (!(memento is RegulationExtrasModel))
        //    {
        //        throw new Exception("Unknown memento class " + memento.ToString());
        //    }

        //    this._state = memento.GetState();
        //    log.Debug($"EmbeddedTableParser: My state has changed to: {_state}");
        //}
    }
}
