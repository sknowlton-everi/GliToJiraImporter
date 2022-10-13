using Syncfusion.DocIO.DLS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliToJiraImporter.Models
{
    public interface IOriginator
    {
        //IOriginator(string state);
        //void DoSomething();
        //void Parse(IEntity entity);
        IMemento Save();
        void Restore(IMemento memento);
    }
}
