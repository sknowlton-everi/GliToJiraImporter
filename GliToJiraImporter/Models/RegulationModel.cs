using log4net;
using System.Reflection;
using System.Text.Json;

namespace GliToJiraImporter.Models
{
    public class RegulationModel : IMemento
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        public ClauseIdModel ClauseId { get; set; } = new ClauseIdModel();
        public string Subcategory { get; set; } = string.Empty;
        public DescriptionModel Description { get; set; } = new DescriptionModel();

        public RegulationModel() { }

        public RegulationModel(RegulationModel state)
        {
            this.ClauseId = state.ClauseId;
            this.Subcategory = state.Subcategory;
            this.Description = state.Description;
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
            return !this.IsEmpty() && this.ClauseId.IsValid() && Description.IsValid();
        }

        public bool IsEmpty()
        {
            return this.ClauseId.IsEmpty() && Description.IsEmpty() && Subcategory.Trim().Equals(string.Empty);// && AttachmentList.Count == 0; //TODO Cleanup
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
                    result = this.ClauseId.Equals(inputModel.ClauseId);
                    result = result && this.Subcategory.Equals(inputModel.Subcategory);
                    result = result && this.Description.Equals(inputModel.Description);
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