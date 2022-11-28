using log4net;
using GliToJiraImporter.Extensions;
using Syncfusion.DocIO.DLS;
using System.Reflection;
using Microsoft.SqlServer.Management.HadrModel;

namespace GliToJiraImporter.Parsers
{
    public class EmbeddedTableParser
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private string _state = string.Empty;

        public EmbeddedTableParser()
        { }

        public EmbeddedTableParser(string state)
        {
            if (state == null)
            {
                this._state = string.Empty;
            }
            else
            {
                this._state = state;
            }
            log.Debug("EmbeddedTableParser: My initial state is: " + this._state);
        }

        public void Parse(WTableCell cell)
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

        public bool IsValid()
        {
            if (!this._state.Equals(string.Empty))
            {
                bool result = true;
                string[] formattedTableRows = this._state.Split('\n');

                // Check for the column headers and that there is at least one row
                result = formattedTableRows[0].IsValidHeaderRowFormatting();
                if (!result)
                {
                    return false;
                }

                string[] headers = formattedTableRows[0].Substring(2, formattedTableRows[0].Length - 4).Split("||");
                for (int i = 0; i < headers.Length && result; i++)
                {
                    result = !headers[i].Trim().Equals(string.Empty);
                }

                // Check that every row is formated correctly and the number of columns match
                for (int i = 1; i < formattedTableRows.Length && result; i++)
                {
                    result = formattedTableRows[i].IsValidRowFormatting(headers.Length);
                    if (!result)
                    {
                        break;
                    }
                }

                return result;
            }
            return false;
        }

        public string Save()
        {
            return this._state;
        }

        public void Restore(string state)
        {
            this._state = state;
            log.Debug($"EmbeddedTableParser: My state has changed to: {this._state}");
        }
    }
}