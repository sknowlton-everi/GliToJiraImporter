using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliToJiraImporter.Models
{
    public interface IMemento
    {
        string GetName();//TODO Should this be renamed because their names aren't called Name?
        IMemento GetState();
        bool IsValid();
        bool IsEmpty();
    }
}
