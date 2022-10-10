using System.Text.Json;

namespace GliToJiraImporter.Models
{
    public class CategoryModel : IMemento
    {
        public string Category { get; set; } = string.Empty;
        public IList<RegulationModel> RegulationList { get; set; } = new List<RegulationModel>();
        //public IList<IMemento> RegulationList { get; set; } = (IList<IMemento>)new List<RegulationModel>();
        //TODO Should ln:8 be IMemento instead, like ln:9^?

        public CategoryModel() { }

        public CategoryModel(string state)
        {
            string[] splitState = state.Split("|||");
            Category = splitState[0];
            RegulationList.Clear();
            for (int i = 1; i < splitState.Length; i++)
            {
                RegulationList.Add(new RegulationModel(splitState[i]));
            }
        }

        public bool IsValid()
        {
            bool result = !IsEmpty() && !Category.Equals(string.Empty) && RegulationList.Count != 0;
            if (result)
            {
                for (int i = 0; i < RegulationList.Count && result; i++)
                {
                    result = RegulationList[i].IsValid();
                }
            }
            return result;
        }

        public bool IsEmpty()
        {
            bool result = Category.Equals(string.Empty) && RegulationList.Count == 0;
            if (result)
            {
                for (int i = 0; i < RegulationList.Count && result; i++)
                {
                    result = RegulationList[i].IsEmpty();
                }
            }
            return result;
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        //public string GetState()
        //{
        //    string state = $"{Category}";
        //    foreach (RegulationModel regulationModel in RegulationList)
        //    {
        //        state += $"|||{regulationModel.GetState()}";
        //    }
        //    return state;
        //}

        public IMemento GetState()
        {
            return this;
        }
        public string GetName()
        {
            return $"{this.Category}";
        }
    }
}
