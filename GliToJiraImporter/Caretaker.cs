using GliToJiraImporter.Models;
using log4net;
using System.Reflection;
using System.Text.Json;

namespace GliToJiraImporter
{
    // The Caretaker doesn't depend on the Concrete Memento class. Therefore, it
    // doesn't have access to the originator's state, stored inside the memento.
    // It works with all mementos via the base Memento interface.
    public class Caretaker
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);

        private readonly List<IMemento> _mementos = new();

        private readonly IOriginator _originator;

        public Caretaker(IOriginator originator)
        {
            this._originator = originator;
        }

        public void Backup()
        {
            log.Debug($"Caretaker: Saving Originator {_originator.GetType().Name}'s state...");
            this._mementos.Add(this._originator.Save());
        }

        public void Undo()
        {
            if (this._mementos.Count == 0)
            {
                return;
            }

            IMemento memento = this._mementos.Last();
            this._mementos.Remove(memento);

            log.Debug("Caretaker: Restoring state to: " + JsonSerializer.Serialize(memento.GetState()));

            try
            {
                this._originator.Restore(memento);
            }
            catch (Exception ex)
            {
                this.Undo();
            }
        }

        public void ShowHistory()
        {
            log.Debug("Caretaker: Here's the list of mementos:");

            foreach (var memento in this._mementos)
            {
                log.Info(JsonSerializer.Serialize(memento.GetState()));
            }
        }
    }
}