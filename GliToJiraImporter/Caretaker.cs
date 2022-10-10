using GliToJiraImporter.Models;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GliToJiraImporter
{
    // The Caretaker doesn't depend on the Concrete Memento class. Therefore, it
    // doesn't have access to the originator's state, stored inside the memento.
    // It works with all mementos via the base Memento interface.
    public class Caretaker
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private List<IMemento> _mementos = new List<IMemento>();

        private IOriginator _originator = null;

        public Caretaker(IOriginator originator)
        {
            this._originator = originator;
        }

        public void Backup()
        {
            log.Debug("Caretaker: Saving Originator's state...");
            this._mementos.Add(this._originator.Save());
        }

        public void Undo()
        {
            if (this._mementos.Count == 0)
            {
                return;
            }

            var memento = this._mementos.Last();
            this._mementos.Remove(memento);

            //log.Debug("Caretaker: Restoring state to: " + memento.GetState()); //GetName()

            try
            {
                this._originator.Restore(memento);
            }
            catch (Exception)
            {
                this.Undo();
            }
        }

        public void ShowHistory()
        {
            log.Debug("Caretaker: Here's the list of mementos:");

            foreach (var memento in this._mementos)
            {
                log.Info(memento.GetState()); //GetName()
            }
        }

    }
}
