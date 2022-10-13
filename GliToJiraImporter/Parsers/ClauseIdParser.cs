using GliToJiraImporter.Models;
using log4net;
using Syncfusion.DocIO.DLS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GliToJiraImporter.Parsers
{
    public class ClauseIdParser : IOriginator
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private RegulationExtrasModel _state = new RegulationExtrasModel();

        public ClauseIdParser() { }

        public ClauseIdParser(RegulationExtrasModel state)
        {
            this._state = state;
            log.Debug("ClauseIdParser: My initial state is: " + JsonSerializer.Serialize(this._state));
            if (this._state == null)
            {
                this._state = new RegulationExtrasModel();
            }
        }

        public void Parse(WParagraph paragraph)
        {
            Regex clauseIdRegex = new Regex(@"((NS)+(\d)+(.)+(\d)+(.)+(\d))");
            if (clauseIdRegex.IsMatch(paragraph.Text))
            {
                this._state.State = paragraph.Text;
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
            log.Debug($"ClauseIdParser: My state has changed to: {JsonSerializer.Serialize(this._state)}");
        }
    }
}
