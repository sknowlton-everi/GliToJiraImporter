using log4net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace GliToJiraImporter.Models
{
    public class ClauseIdModel : IMemento
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        public string BaseClauseId { get; set; } = string.Empty;
        public string FullClauseId { get; set; } = string.Empty;

        public ClauseIdModel() { }

        public ClauseIdModel(ClauseIdModel state)
        {
            this.BaseClauseId = state.BaseClauseId;
            this.FullClauseId = state.FullClauseId;
        }

        public string GetName()
        {
            return this.FullClauseId;
        }

        public IMemento GetState()
        {
            return this;
        }

        public bool IsEmpty()
        {
            return this.BaseClauseId.Trim().Equals(string.Empty) && this.FullClauseId.Trim().Equals(string.Empty);
        }

        public bool IsValid()
        {
            return !this.IsEmpty() && ContainsClauseId(BaseClauseId);
        }

        public static bool ContainsClauseId(string textToCheck)
        {
            bool result = false;
            Regex[] clauseIdRegexs = new Regex[]
            {
                // 3 sets of numbers
                new(@"(([A-Za-z0-9]{0,2})+(-?)+(\d+)+(.)+(\d+)+(.)+(\d+))"),
                new(@"(([A-Za-z0-9]{0,2})+(-?)+(\d+)+(.)+(\d+)+(.)+(\d+)+([(])+(\d+)+([)]))"),
                new(@"(([A-Za-z0-9]{0,2})+(-?)+(\d+)+(.)+(\d+)+(.)+(\d+)+([(])+([A-Za-z0-9])+([)]))"),
                new(@"(([A-Za-z0-9]{0,2})+(-?)+(\d+)+(.)+(\d+)+(.)+(\d+)+([A-Za-z0-9]))"),
                // 2 sets of numbers
                new(@"(([A-Za-z0-9]{0,2})+(-?)+(\d+)+(.)+(\d+))"),
                new(@"(([A-Za-z0-9]{0,2})+(-?)+(\d+)+(.)+(\d+)+([(])+(\d+)+([)]))"),
                new(@"(([A-Za-z0-9]{0,2})+(-?)+(\d+)+(.)+(\d+)+([(])+([A-Za-z0-9])+([)]))"),
                new(@"(([A-Za-z0-9]{0,2})+(-?)+(\d+)+(.)+(\d+)+([A-Za-z0-9]))"),
                new(@"(([A-Za-z0-9]{0,2})+(-?)+(\d+)+([(])+(\d+)+([)])+([(])+([A-Za-z0-9])+([)]))"),
                // 1 set of numbers
                new(@"(([A-Za-z0-9]{0,2})+(-?)+(\d+))"),
                new(@"(([A-Za-z0-9]{0,2})+(-?)+(\d+)+([(])+(\d+)+([)]))"),
                new(@"(([A-Za-z0-9]{0,2})+(-?)+(\d+)+([(])+([A-Za-z0-9])+([)]))"),
                new(@"(([A-Za-z0-9]{0,2})+(-?)+(\d+)+([A-Za-z0-9]))"),
            };

            foreach (Regex regex in clauseIdRegexs)
            {
                Match match = regex.Match(textToCheck);

                if (!match.Success || !match.Value.Equals(textToCheck.Trim())) continue;

                result = true;
            }
            return result;
        }

        public override bool Equals(object? obj)
        {
            bool result = false;
            try
            {
                if (obj != null)
                {
                    ClauseIdModel inputModel = (ClauseIdModel)obj;
                    result = this.BaseClauseId.Equals(inputModel.BaseClauseId);
                    result = result && this.FullClauseId.Equals(inputModel.FullClauseId);
                }
            }
            catch (InvalidCastException)
            {
                log.Error("The passed in object is not of type ClauseIdModel");
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
