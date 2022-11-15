using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliToJiraImporter.Models
{
    public class ErrorRoot
    {
        public List<string> errorMessages { get; set; }
        public List<object> warningMessages { get; set; }
    }
}