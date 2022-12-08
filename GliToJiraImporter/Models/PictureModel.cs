namespace GliToJiraImporter.Models
{
    public class PictureModel : IMemento
    {
        public string ImageName { get; set; } = string.Empty;
        public byte[] ImageBytes { get; set; } = Array.Empty<byte>();

        public PictureModel()
        { }

        public PictureModel(string imageName, byte[] imageBytes)
        {
            this.ImageName = imageName;
            this.ImageBytes = imageBytes;
        }

        public PictureModel(PictureModel state)
        {
            this.ImageName = state.ImageName;
            this.ImageBytes = state.ImageBytes;
        }

        public string GetName()
        {
            return this.ImageName;
        }

        public IMemento GetState()
        {
            return this;
        }

        public bool IsEmpty()
        {
            return !this.ImageBytes.Any();
        }

        public bool IsValid()
        {
            //TODO Should I add something to this?
            return !this.IsEmpty();
        }
    }
}