using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GliToJiraImporter.Models
{
    public class RegulationModel
    {
        public string ClauseID { get; set; } = string.Empty;
        public string Subcategory { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public IList<byte[]> AttachmentList { get; set; } = new List<byte[]>();

        public bool IsValid()
        {
            return !ClauseID.Equals(string.Empty) && !Description.Equals(string.Empty) && !Subcategory.Equals(string.Empty);
        }

        public bool IsEmpty()
        {
            return ClauseID.Equals(string.Empty) && Description.Equals(string.Empty) && Subcategory.Equals(string.Empty) && AttachmentList.Count == 0;
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
