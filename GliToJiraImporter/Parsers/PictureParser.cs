using GliToJiraImporter.Models;
using log4net;
using Syncfusion.DocIO.DLS;
using System.Reflection;
using System.Text.Json;

namespace GliToJiraImporter.Parsers
{
    public class PictureParser : IOriginator
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);

        private PictureModel _state = new();

        public PictureParser() { }

        public PictureParser(IMemento state)
        {
            this._state = (PictureModel)state;
            log.Debug("PictureParser: My initial state is: " + JsonSerializer.Serialize(this._state));
        }

        public void Parse(WParagraph paragraph)
        {
            for (int i = 0; i < paragraph.ChildEntities.Count; i++)
            {
                if (paragraph.ChildEntities[i].GetType().Equals(typeof(WPicture)))
                {
                    WPicture picture = (WPicture)paragraph.ChildEntities[i];
                    this._state = new PictureModel($"{picture.Name.Replace(' ', '-')}.png", picture.ImageBytes);
                }
            }
        }

        // Saves the current state inside a memento.
        public IMemento Save()
        {
            return new PictureModel(this._state);
        }

        // Restores the Originator's state from a memento object.
        public void Restore(IMemento memento)
        {
            if (memento is not PictureModel)
            {
                throw new Exception("Unknown memento class " + memento);
            }

            this._state = (PictureModel)memento.GetState();
            log.Debug($"PictureParser: My state has changed to: {JsonSerializer.Serialize(this._state)}");
        }
    }
}