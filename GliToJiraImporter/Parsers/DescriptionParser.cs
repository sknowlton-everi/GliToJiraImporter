using GliToJiraImporter.Models;
using log4net;
using Syncfusion.DocIO.DLS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
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
            log.Debug("DescriptionParser: My initial state is: " + JsonSerializer.Serialize(this._state));
            if (this._state == null)
            {
                this._state = new RegulationExtrasModel();
            }
        }

        public void Parse(WParagraph paragraph)
        {
            string result = string.Empty;
            if (!this._state.State.Equals(string.Empty))
            {
                this._state.State += "\n";
            }

            // Checking for a dash (-) in front of and behind a word, then added spaces to avoid Jira crossing it out
            string strikeThroughRegex = @"([^A-Za-z0-9])(-)([^\s].+[^\s])(-)([^A-Za-z0-9])";
            result = Regex.Replace(paragraph.Text, strikeThroughRegex, "$1\\$2$3\\$4$5");

            // Check for different text styling
            WParagraphFormat paragraphFormat = paragraph.ParagraphFormat;
            for (int i = 0; i < paragraph.Items.Count; i++)
            {
                if (paragraph.Items[i].GetType() == typeof(Break))
                {
                    result += '\n';
                }
                else
                {
                    //TODO whitespace might cause issues with all of these but I'm worried that trimming will remove any spacing between this and the other ranges
                    WTextRange textRange = (WTextRange)paragraph.Items[i];
                    if (textRange.CharacterFormat.Bold)
                    {
                        if (textRange.PreviousSibling == null || (textRange.PreviousSibling.GetType() == typeof(WTextRange) && !((WTextRange)textRange.PreviousSibling).CharacterFormat.Bold))
                        {
                            result = result.Replace(textRange.Text, $"*{textRange.Text}");
                        }
                        if (textRange.NextSibling == null || (textRange.NextSibling.GetType() == typeof(WTextRange) && !((WTextRange)textRange.NextSibling).CharacterFormat.Bold))
                        {
                            result = result.Replace(textRange.Text, $"{textRange.Text}*");
                        }
                    }
                    if (textRange.CharacterFormat.Italic)
                    {
                        if (textRange.PreviousSibling == null || (textRange.PreviousSibling.GetType() == typeof(WTextRange) && !((WTextRange)textRange.PreviousSibling).CharacterFormat.Italic))
                        {
                            result = result.Replace(textRange.Text, $"_{textRange.Text}");
                        }
                        if (textRange.NextSibling == null || (textRange.NextSibling.GetType() == typeof(WTextRange) && !((WTextRange)textRange.NextSibling).CharacterFormat.Italic))
                        {
                            result = result.Replace(textRange.Text, $"{textRange.Text}_");
                        }
                    }
                    if (textRange.CharacterFormat.Strikeout)
                    {
                        if (textRange.PreviousSibling == null || (textRange.PreviousSibling.GetType() == typeof(WTextRange) && !((WTextRange)textRange.PreviousSibling).CharacterFormat.Strikeout))
                        {
                            result = result.Replace(textRange.Text, $"-{textRange.Text}");
                        }
                        if (textRange.NextSibling == null || (textRange.NextSibling.GetType() == typeof(WTextRange) && !((WTextRange)textRange.NextSibling).CharacterFormat.Strikeout))
                        {
                            result = result.Replace(textRange.Text, $"{textRange.Text}-");
                        }
                    }
                    if (textRange.CharacterFormat.UnderlineStyle == UnderlineStyle.Single)
                    {
                        if (textRange.PreviousSibling == null || (textRange.PreviousSibling.GetType() == typeof(WTextRange) && ((WTextRange)textRange.PreviousSibling).CharacterFormat.UnderlineStyle != UnderlineStyle.Single))
                        {
                            result = result.Replace(textRange.Text, $"+{textRange.Text}");
                        }
                        if (textRange.NextSibling == null || (textRange.NextSibling.GetType() == typeof(WTextRange) && ((WTextRange)textRange.NextSibling).CharacterFormat.UnderlineStyle != UnderlineStyle.Single))
                        {
                            result = result.Replace(textRange.Text, $"{textRange.Text}+");
                        }
                    }
                }
            }

            // Checking for tabs and what I guess are dashes, as Jira doesn't know how to read them
            result = result.Replace("\u000B", "&nbsp;&nbsp;&nbsp;&nbsp;");
            result = result.Replace('\u001E', '-');

            this._state.State += result;
        }

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
            log.Debug($"DescriptionParser: My state has changed to: {JsonSerializer.Serialize(this._state)}");
        }
    }
}
