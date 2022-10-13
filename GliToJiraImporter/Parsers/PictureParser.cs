using GliToJiraImporter.Models;
using log4net;
using Syncfusion.DocIO.DLS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GliToJiraImporter.Parsers
{
    public class PictureParser : IOriginator
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private PictureModel _state = new PictureModel();

        public PictureParser() { }

        public PictureParser(PictureModel state)
        {
            this._state = state;
            log.Debug("PictureParser: My initial state is: " + JsonSerializer.Serialize(this._state));
            if (this._state == null)
            {
                this._state = new PictureModel();
            }
        }

        public void Parse(WParagraph paragraph)
        {
            for (int i = 0; i < paragraph.ChildEntities.Count; i++)
            {
                if (paragraph.ChildEntities[i].GetType().Equals(typeof(WPicture)))
                {
                    WPicture picture = (WPicture)paragraph.ChildEntities[i];
                    this._state = new PictureModel(picture.Name, picture.ImageBytes);
                }
            }
        }

        // Saves the current state inside a memento.
        public IMemento Save()
        {
            return this._state;
        }

        //Restores the Originator's state from a memento object.
        public void Restore(IMemento memento)
        {
            if (!(memento is PictureModel))
            {
                throw new Exception("Unknown memento class " + memento.ToString());
            }

            this._state = (PictureModel)memento.GetState();
            log.Debug($"PictureParser: My state has changed to: {JsonSerializer.Serialize(this._state)}");
        }
    }
}
