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
            return !this.IsEmpty();//TODO add attachment validating
        }

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