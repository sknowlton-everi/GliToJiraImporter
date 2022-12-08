using System.Text.Json;

namespace GliToJiraImporter.Models
{
    public class CategoryModel : IMemento
    {
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
    }
}