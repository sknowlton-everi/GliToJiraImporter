using GliToJiraImporter.Models;
using log4net;
using Syncfusion.DocIO.DLS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GliToJiraImporter.Parsers
{
    public class PictureParser //: IOriginator
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private byte[] _state = new byte[0];

        public PictureParser() { }

        public PictureParser(byte[] state)
        {
            this._state = state;
            log.Debug("PictureParser: My initial state is: " + state);
            if (this._state == null)
            {
                this._state = new byte[0];
            }
        }
        public void Parse(WParagraph paragraph)//TODO change to just be a string of the text???
        {
            for (int i = 0; i < paragraph.ChildEntities.Count; i++)
            {
                if (paragraph.ChildEntities[i].GetType().Equals(typeof(WPicture)))
                {
                    WPicture picture = (WPicture)paragraph.ChildEntities[i];
                    this._state = picture.ImageBytes;
                }
            }
        }

        // Saves the current state inside a memento.
        public byte[] Save()
        {
            return this._state;
        }

        ////Restores the Originator's state from a memento object.
        //public void Restore(IMemento memento)
        //{
        //    if (!(memento is RegulationExtrasModel))
        //    {
        //        throw new Exception("Unknown memento class " + memento.ToString());
        //    }

        //    this._state = (RegulationExtrasModel)memento.GetState();
        //    log.Debug($"PictureParser: My state has changed to: {_state}");
        //}
    }
}
