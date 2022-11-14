namespace GliToJiraImporter.Models
{
    public class EmbeddedTableModel : IMemento
    {
        public IList<string> Headers { get; set; } = new List<string>();
        public IList<string> ValueRows { get; set; } = new List<string>();
        public string FormattedTable { get; set; } = string.Empty;

        public EmbeddedTableModel() { }

        public EmbeddedTableModel(EmbeddedTableModel state)
        {
            this.Headers = state.Headers;
            this.ValueRows = state.ValueRows;
            this.FormattedTable = state.FormattedTable;
        }

        public string GetName()
        {
            return this.FormattedTable;
        }

        public IMemento GetState()
        {
            return this;
        }

        public bool IsValid()
        {
            bool isValid = !this.IsEmpty();
            string[] formattedTableRows = this.FormattedTable.Split('\n');

            // Check for the column headers and that there is at least one row
            isValid = isValid && formattedTableRows[0].Trim().StartsWith("||") && formattedTableRows[0].Trim().EndsWith("||");
            isValid = isValid && formattedTableRows.Length >= 2;

            string[] headers = formattedTableRows[0].Substring(2, formattedTableRows[0].Length - 4).Split("||"); //.Remove(0, 2).Remove(rows[0].Length - 3, 2).Split("||"); //TODO cleanup
            for (int i = 0; i < this.Headers.Count && isValid; i++)
            {
                isValid = isValid && !this.Headers[i].Trim().Equals(string.Empty);
                isValid = isValid && this.Headers[i].Equals(headers[i]);
            }

            // Check that every row is formated correctly and the number of columns match
            for (int i = 1; i < formattedTableRows.Length && isValid; i++)
            {
                isValid = isValid && !formattedTableRows[i].Trim().Equals(string.Empty);
                isValid = isValid && this.ValueRows[i - 1].Equals(formattedTableRows[i]);

                isValid = isValid && formattedTableRows[i].Trim().StartsWith("|") && formattedTableRows[i].Trim().EndsWith("|");
                isValid = isValid && headers.Length == formattedTableRows[i].Substring(1, formattedTableRows[i].Length - 2).Split("|").Length;
            }
            return isValid;
        }

        public bool IsEmpty()
        {
            return this.Headers.Count == 0 && this.ValueRows.Count == 0 && this.FormattedTable.Equals(string.Empty);
        }
    }
}
