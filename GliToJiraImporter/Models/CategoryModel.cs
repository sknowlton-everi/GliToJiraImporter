using log4net;
using System.Reflection;
using System.Text.Json;

namespace GliToJiraImporter.Models
{
    public class CategoryModel : IMemento
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        public string Category { get; set; } = string.Empty;
        public IList<RegulationModel> RegulationList { get; set; } = new List<RegulationModel>();
        public bool NoCategory { get; set; }
        //public IList<IMemento> RegulationList { get; set; } = (IList<IMemento>)new List<RegulationModel>();
        //TODO Should ln:8 be IMemento instead, like ln:9^?

        public CategoryModel() { }

        public CategoryModel(CategoryModel state)
        {
            this.Category = state.Category;
            this.RegulationList = state.RegulationList;
            this.NoCategory = state.NoCategory;
        }

        public bool IsValid()
        {
            bool result = !IsEmpty() && !Category.Equals(string.Empty) && RegulationList.Count != 0;

            if (!result) return result;

            for (int i = 0; i < RegulationList.Count && result; i++)
            {
                result = RegulationList[i].IsValid() && (NoCategory || !RegulationList[i].Subcategory.Equals(string.Empty));
            }
            return result;
        }

        public bool IsEmpty()
        {
            bool result = Category.Trim().Equals(string.Empty) && RegulationList.Count == 0;

            if (!result) return result;

            for (int i = 0; i < RegulationList.Count && result; i++)
            {
                result = RegulationList[i].IsEmpty();
            }
            return result;
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public IMemento GetState()
        {
            return this;
        }

        public string GetName()
        {
            return this.Category;
        }

        //TODO this equals would break if the regulation lists were sorted differently
        public override bool Equals(object? obj)
        {
            bool result = false;
            try
            {
                if (obj != null)
                {
                    CategoryModel inputModel = (CategoryModel)obj;
                    result = this.Category.Equals(inputModel.Category);
                    if (!result)
                    {
                        return false;
                    }
                    result = this.RegulationList.Count.Equals(inputModel.RegulationList.Count);

                    for (int i = 0; i < this.RegulationList.Count && result; i++)
                    {
                        result = this.RegulationList[i].Equals(inputModel.RegulationList[i]);
                    }
                }
            }
            catch (InvalidCastException)
            {
                log.Error("The passed in object is not of type CategoryModel");
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