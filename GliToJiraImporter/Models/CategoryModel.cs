using System.Text.Json;

namespace GliToJiraImporter.Models
{
    public class CategoryModel
    {
        public string Category { get; set; } = string.Empty;
        public IList<RegulationModel> RegulationList { get; set; } = new List<RegulationModel>();

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
            bool result = false;
            result = Category.Equals(string.Empty) && RegulationList.Count == 0;
            for (int i = 0; i < RegulationList.Count && !result; i++)
            {
                result = RegulationList[i].IsEmpty();
            }
            return result;
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
