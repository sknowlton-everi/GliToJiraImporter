using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GliToJiraImporter.Models
{
    public class RegulationModel : IMemento
    {
        public string ClauseID { get; set; } = string.Empty;
        public string Subcategory { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IList<PictureModel> AttachmentList { get; set; } = new List<PictureModel>();

        public RegulationModel() { }
        public RegulationModel(string state)
        {
            if (!state.Equals(string.Empty))
            {
                string[] splitState = state.Split("///");
                ClauseID = splitState[0];
                Subcategory = splitState[1];
                Description = splitState[2];
            }
        }

        public string GetName()
        {
            return this.Subcategory;
        }

        public IMemento GetState()
        {
            return this;
        }

        public bool IsValid()
        {
            return !ClauseID.Equals(string.Empty) && !Description.Equals(string.Empty) && !Subcategory.Equals(string.Empty);
        }

        public bool IsEmpty()
        {
            return ClauseID.Equals(string.Empty) && Description.Equals(string.Empty) && Subcategory.Equals(string.Empty) && AttachmentList.Count == 0;
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
