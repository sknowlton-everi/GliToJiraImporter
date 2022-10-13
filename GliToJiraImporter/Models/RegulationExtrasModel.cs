using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GliToJiraImporter.Models
{
    public class RegulationExtrasModel : IMemento
    {
        public string State { get; set; } = string.Empty;

        public RegulationExtrasModel() { }

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
            //bool isClauseId = new Regex(@"((NS)+(\d)+(.)+(\d)+(.)+(\d))").IsMatch(this.State);
            //bool isPicture = Convert.TryFromBase64String(this.State, new Span<byte>(), out int bytesWritten);
            //bool isEmbededdTable = ;
            //bool isDescription = !this.IsEmpty();
            
            //return isClauseId || isPicture || isEmbededdTable || isDescription;
            return IsValidClauseId() || IsValidDescription();
        }

        public bool IsValidClauseId()
        {
            return !this.IsEmpty() && new Regex(@"((NS)+(\d)+(.)+(\d)+(.)+(\d))").IsMatch(this.State);
        }

        public bool IsValidDescription()
        {
            return !this.IsEmpty();
        }
    }
}
