using GliToJiraImporter.Models;
using log4net;
using Syncfusion.DocIO.DLS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GliToJiraImporter.Parsers
{
    public class DescriptionParser : IOriginator
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private RegulationExtrasModel _state = new RegulationExtrasModel();

        public DescriptionParser() { }

        public DescriptionParser(RegulationExtrasModel state)
        {
            this._state = state;
            log.Debug("DescriptionParser: My initial state is: " + state);
            if (this._state == null)
            {
                this._state = new RegulationExtrasModel();
            }
        }

        public void Parse(WParagraph paragraph)//TODO change to just be a string of the text???
        {
            string description;
            if (!this._state.State.Equals(string.Empty))
            {
                this._state.State += "\n";
            }

            // Checking for a dash (-) in front of and behind a word, then added spaces to avoid Jira crossing it out
            //string strikeThroughRegex = @"([^A-Za-z0-9])(-)([^\s].+[^\s])(-)([^A-Za-z0-9])";
            //paragraph.Text = Regex.Replace(paragraph.Text, strikeThroughRegex, "$1 $2 $3 $4 $5");

            // Check for different text styling
            WParagraphFormat paragraphFormat = paragraph.ParagraphFormat;
            for (int i = 0; i < paragraph.Items.Count; i++)
            //foreach(WTextRange textRange in paragraph.Items)
            {
                //TODO whitespace might cause issues with all of these but I'm worried that trimming will remove any spacing between this and th other ranges
                WTextRange textRange = (WTextRange)paragraph.Items[i];
                if (textRange.CharacterFormat.Bold)
                {
                    textRange.Text = $"*{textRange.Text}*";
                }
                if (textRange.CharacterFormat.Italic)
                {
                    textRange.Text = $"_{textRange.Text}_";
                }
                if (textRange.CharacterFormat.Strikeout)
                {
                    textRange.Text = $"-{textRange.Text}-";
                }
                if (textRange.CharacterFormat.UnderlineStyle == UnderlineStyle.Single)
                {
                    textRange.Text = $"+{textRange.Text}+";
                }
                //System.Drawing.Color blackColor = System.Drawing.Color.Black;
                //if (textRange.CharacterFormat.TextColor != System.Drawing.Color.Black)
                //{
                //    //string rgb = textRange.CharacterFormat.TextColor.R + textRange.CharacterFormat.TextColor.G + textRange.CharacterFormat.TextColor.B;
                //    string rgb = textRange.CharacterFormat.TextColor.ToString();
                //    textRange.Text = $"\\{{color: {rgb}\\}}{textRange.Text}\\{{color\\}}";
                //}
            }


            // Checking for tabs and what I guess are dashes, as Jira doesn't know how to read them
            paragraph.Text = paragraph.Text.Replace("\u000B", "&nbsp;&nbsp;&nbsp;&nbsp;");
            paragraph.Text = paragraph.Text.Replace('\u001E', '-');

            this._state.State += paragraph.Text;
        }

        //// Saves the current state of the clauseId.
        //public string Save() //TODO Can this be string or does it have to be IMemento
        //{
        //    return this._state;
        //}

        // Saves the current state inside a memento.
        public IMemento Save()
        {
            return this._state;
        }

        // Restores the Originator's state from a memento object.
        public void Restore(IMemento memento)
        {
            if (!(memento is RegulationExtrasModel))
            {
                throw new Exception("Unknown memento class " + memento.ToString());
            }

            this._state = (RegulationExtrasModel)memento.GetState();
            log.Debug($"DescriptionParser: My state has changed to: {_state}");
        }
    }
}
