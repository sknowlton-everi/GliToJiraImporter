using log4net;
using System.Reflection;

namespace GliToJiraImporter.Models
{
    public class DescriptionModel : IMemento
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public string Text { get; set; } = string.Empty;
        public IList<PictureModel> AttachmentList { get; set; } = new List<PictureModel>();

        public DescriptionModel() { }

        public DescriptionModel(string state)
        {
            this.Text = state;
        }

        public DescriptionModel(DescriptionModel state)
        {
            this.Text = state.Text;
            this.AttachmentList = state.AttachmentList;
        }

        public string GetName()
        {
            return this.Text;
        }

        public IMemento GetState()
        {
            return this;
        }

        public bool IsEmpty()
        {
            return this.Text.Trim().Equals(string.Empty) && this.AttachmentList.Count == 0;
        }

        public bool IsValid()
        {
            return !this.IsEmpty();// this.IsValidEmbeddedTable()); //TODO Cleanup //TODO add attachment validating
        }

        //TODO cleanup
        //public bool IsValidEmbeddedTable()
        //{
        //    bool isValid = true;
        //    if (this.IsEmpty())
        //    {
        //        isValid = false;
        //    }
        //    else
        //    {
        //        string[] lines = this.Text.Split('\n');
        //        // Check for the column headers and that there is at least one row
        //        isValid = isValid && lines[0].Trim().StartsWith("||") && lines[0].Trim().EndsWith("||");
        //        isValid = isValid && lines.Length >= 2;
        //        int numOfColumns = lines[0].Split("||").Length;

        //        // Check that every row is formated correctly and the number of columns match
        //        for (int i = 1; i < lines.Length; i++)
        //        {
        //            if (lines[i].Trim().Equals(string.Empty))
        //            {
        //                isValid = false;
        //            }
        //            isValid = isValid && lines[i].Trim().StartsWith("|") && lines[i].Trim().EndsWith("|");
        //            isValid = isValid && numOfColumns == lines[0].Split("|").Length;
        //        }
        //    }
        //    return isValid;
        //}

        public override bool Equals(object? obj)
        {
            bool result = false;
            try
            {
                if (obj != null)
                {
                    DescriptionModel inputModel = (DescriptionModel)obj;
                    result = this.Text.Equals(inputModel.Text);
                    result = result && this.AttachmentList.Count == inputModel.AttachmentList.Count;
                    for (int i = 0; i < this.AttachmentList.Count && result; i++)
                    {
                        result = result && this.AttachmentList[i].Equals(inputModel.AttachmentList[i]);
                    }
                }
            }
            catch (InvalidCastException)
            {
                log.Error("The passed in object is not of type DescriptionModel");
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