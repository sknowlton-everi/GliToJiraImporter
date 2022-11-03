using log4net;
using System.Reflection;
using System.Text.Json;

namespace GliToJiraImporter.Models
{
    public class RegulationModel : IMemento
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public string ClauseID { get; set; } = string.Empty;
        public string Subcategory { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IList<PictureModel> AttachmentList { get; set; } = new List<PictureModel>();

        public RegulationModel()
        { }

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
            return !ClauseID.Equals(string.Empty) && !Description.Equals(string.Empty);
        }

        public bool IsEmpty()
        {
            return ClauseID.Equals(string.Empty) && Description.Equals(string.Empty) && Subcategory.Equals(string.Empty) && AttachmentList.Count == 0;
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public override bool Equals(object? obj)
        {
            bool result = false;
            try
            {
                if (obj != null)
                {
                    RegulationModel inputModel = (RegulationModel)obj;
                    result = this.ClauseID.Equals(inputModel.ClauseID);
                    result = result && this.Subcategory.Equals(inputModel.Subcategory);
                    result = result && this.Description.Equals(inputModel.Description);
                    result = result && this.AttachmentList.Count == inputModel.AttachmentList.Count;
                    for (int i = 0; i < this.AttachmentList.Count && result; i++)
                    {
                        result = result && this.AttachmentList[i].Equals(inputModel.AttachmentList[i]);
                    }
                }
            }
            catch (InvalidCastException)
            {
                log.Error("The passed in object is not of type RegulationModel");
                return false;
            }
            return result;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}