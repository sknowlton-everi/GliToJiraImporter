using System.Text.RegularExpressions;

namespace GliToJiraImporter.Models
{
    public class RegulationExtrasModel : IMemento
    {
        public string State { get; set; } = string.Empty;

        public RegulationExtrasModel()
        { }

        public RegulationExtrasModel(string state)
        {
            this.State = state;
        }

        public string GetName()
        {
            return this.State;
        }

        public IMemento GetState()
        {
            return this;
        }

        public bool IsEmpty()
        {
            return this.State.Equals(string.Empty);
        }

        public bool IsValid()
        {
            return this.IsValidClauseId() || this.IsValidDescription() || this.IsValidEmbeddedTable();
        }

        public bool IsValidClauseId()
        {
            return !this.IsEmpty() && new Regex(@"((NS)+(\d)+(.)+(\d)+(.)+(\d))").IsMatch(this.State);
        }

        public bool IsValidDescription()
        {
            return !this.IsEmpty(); //TODO How should I check if description is valid?
        }

        public bool IsValidEmbeddedTable()
        {
            bool isValid = true;
            if (this.IsEmpty())
            {
                isValid = false;
            }
            else
            {
                string[] lines = this.State.Split('\n');
                // Check for the column headers and that there is at least one row
                isValid = isValid && lines[0].Trim().StartsWith("||") && lines[0].Trim().EndsWith("||");
                isValid = isValid && lines.Length >= 2;
                int numOfColumns = lines[0].Split("||").Length;

                // Check that every row is formated correctly and the number of columns match
                for (int i = 1; i < lines.Length; i++)
                {
                    if (lines[i].Trim().Equals(string.Empty))
                    {
                        isValid = false;
                    }
                    isValid = isValid && lines[i].Trim().StartsWith("|") && lines[i].Trim().EndsWith("|");
                    isValid = isValid && numOfColumns == lines[0].Split("|").Length;
                }
            }
            return isValid;
        }
    }
}