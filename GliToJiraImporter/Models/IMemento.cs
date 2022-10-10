using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliToJiraImporter.Models
{
    public interface IMemento
    {
        string GetName();//TODO not necessary? because their names aren't called Name
        IMemento GetState();
        //DateTime GetDate();
        bool IsValid();
        bool IsEmpty();
    }
}
