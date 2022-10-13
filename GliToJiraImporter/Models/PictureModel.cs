using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GliToJiraImporter.Models
{
    public class PictureModel : IMemento
    {
        public string ImageName { get; set; } = string.Empty;
        public byte[] ImageBytes { get; set; } = new byte[] { };

        public PictureModel() { }

        public PictureModel(string imageName, byte[] imageBytes)
        {
            this.ImageName = imageName;
            this.ImageBytes = imageBytes;
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
            return !ImageBytes.Any();
        }

        public bool IsValid()
        {
            return !this.IsEmpty(); //TODO Could I add something to this?
        }
    }
}
