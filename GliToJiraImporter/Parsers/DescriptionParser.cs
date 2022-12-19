using GliToJiraImporter.Models;
using GliToJiraImporter.Utilities;
using log4net;
using Syncfusion.DocIO.DLS;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GliToJiraImporter.Parsers
{
    public class DescriptionParser : IOriginator
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private DescriptionModel _state = new();

        public DescriptionParser() { }

        public DescriptionParser(IMemento state)
        {
            if (state == null)
            {
                this._state = new DescriptionModel();
            }
            else
            {
                this._state = (DescriptionModel)state;
            }
            log.Debug("DescriptionParser: My initial state is: " + JsonSerializer.Serialize(this._state));
        }

        public void Parse(WTableCell cell)
        {
            string result = string.Empty;

            PictureParser pictureParser = new PictureParser();
            Caretaker pictureCaretaker = new Caretaker(pictureParser);
            EmbeddedTableParser embeddedTableParser = new EmbeddedTableParser();

            // Iterates through the paragraphs of the cell
            for (int i = 0; i < cell.Paragraphs.Count; i++)
            {
                if (!result.Trim().Equals(string.Empty))
                {
                    result += '\n';
                }

                if (!cell.Paragraphs[i].Text.Equals(string.Empty) && !cell.Paragraphs[i].Text.Contains("Choose an item"))
                {
                    result += this.parseParagraph(cell.Paragraphs[i]);
                }
                // Checks for a picture within a cell 
                else if (cell.Paragraphs[i].ChildEntities.Count != 0)
                {
                    pictureCaretaker.Backup();
                    pictureParser.Parse(cell.Paragraphs[i]);
                    if (!pictureParser.Save().IsValid())
                    {
                        pictureCaretaker.Undo();
                    }
                    else
                    {
                        this._state.AttachmentList.Add((PictureModel)pictureParser.Save());
                        result += $"(Image included below, Name: {((PictureModel)pictureParser.Save()).ImageName})";
                    }
                }
                // Checks for a table within a cell 
                else if (cell.Tables.Count != 0)
                {
                    string embeddedTableParserBackup = embeddedTableParser.Save();
                    embeddedTableParser.Parse(cell);
                    if (!embeddedTableParser.IsValid())
                    {
                        embeddedTableParser = new EmbeddedTableParser(embeddedTableParserBackup);
                    }
                    else
                    {
                        // Add the embedded table to the end of the description
                        result += embeddedTableParser.Save();
                    }
                }
            }

            // Checking for what I guess are dashes, as Jira doesn't know how to read them
            result = result.Replace('\u001E', '-');

            if (!this._state.Text.Equals(string.Empty) && !result.Trim('\n').Equals(string.Empty))
            {
                this._state.Text += '\n';
            }

            this._state.Text += result;
        }

        private string parseParagraph(WParagraph paragraph)
        {
            string result = string.Empty;

            // Check for different paragraph item types
            for (int i = 0; i < paragraph.Items.Count; i++)
            {
                if (paragraph.Items[i].GetType() == typeof(Break))
                {
                    result += '\n';
                }
                else if (paragraph.Items[i].GetType() == typeof(WTextRange))
                {
                    WTextRange textRange = (WTextRange)paragraph.Items[i];

                    // Checking for certain characters in front of and behind a word, then added spaces to avoid Jira confusing them for formating
                    textRange = this.ignoreUnintendedFormatting(textRange);

                    // Checking for formatting like bolding, and adding the characters needed for Jira to know about it
                    textRange = this.checkForIntendedFormatting(textRange);

                    result += textRange.Text;
                }
                else if (paragraph.Items[i].GetType() == typeof(WField) && ((WField)paragraph.Items[i]).FieldType == Syncfusion.DocIO.FieldType.FieldHyperlink)
                {
                    WField field = (WField)paragraph.Items[i];
                    string fieldValue = field.FieldValue.Replace("\"", "").Trim();

                    // Prevent the loss of any starting spaces
                    if (field.Text.StartsWith(" "))
                    {
                        result += ' ';
                    }

                    if (LinkUtilities.IsValidWebLink(fieldValue) || LinkUtilities.IsValidEmailAddress(fieldValue))
                    {
                        if (field.Text.Equals(fieldValue))
                        {
                            result += $"[{fieldValue}]";
                        }
                        else
                        {
                            result += $"[{field.Text.Trim()}|{fieldValue}]";
                        }
                    }
                    else
                    {
                        log.Debug($"Link type is not accounted for. Field Text: \"{field.Text}\", Field Value: \"{field.FieldValue}\"");
                        result += $"{field.Text.Trim()}";
                    }

                    // Prevent the loss of any ending spaces
                    if (field.Text.EndsWith(" "))
                    {
                        result += ' ';
                    }

                    // Skip all other field marks 
                    for (int j = i + 1; j < paragraph.Items.Count; j++)
                    {
                        if (paragraph.Items[j].GetType() == typeof(WFieldMark) && ((WFieldMark)paragraph.Items[j]).Type == FieldMarkType.FieldEnd)
                        {
                            i = j;
                            break;
                        }
                    }
                }
                else
                {
                    log.Info($"Type {paragraph.Items[i].EntityType} is not accounted for");
                    result += paragraph.Text;
                }
            }

            return result;
        }

        private WTextRange ignoreUnintendedFormatting(WTextRange textRange)
        {
            string[] formatingChars = { @"\*", "_", "-", @"\+" };

            foreach (string character in formatingChars)
            {
                string formatingCharRegex = "([^A-Za-z0-9])(" + character + @")([^\s].+[^\s])(" + character + ")([^A-Za-z0-9])";
                textRange.Text = Regex.Replace(textRange.Text, formatingCharRegex, "$1\\$2$3\\$4$5");
            }

            return textRange;
        }

        private WTextRange checkForIntendedFormatting(WTextRange textRange)
        {
            if (textRange.CharacterFormat.Bold)
            {
                if (textRange.PreviousSibling == null || (textRange.PreviousSibling.GetType() == typeof(WTextRange) && !((WTextRange)textRange.PreviousSibling).CharacterFormat.Bold))
                {
                    textRange.Text = textRange.Text.Replace(textRange.Text, $"*{textRange.Text}");
                }
                if (textRange.NextSibling == null || (textRange.NextSibling.GetType() == typeof(WTextRange) && !((WTextRange)textRange.NextSibling).CharacterFormat.Bold))
                {
                    textRange.Text = textRange.Text.Replace(textRange.Text, $"{textRange.Text}*");
                }
            }
            if (textRange.CharacterFormat.Italic)
            {
                if (textRange.PreviousSibling == null || (textRange.PreviousSibling.GetType() == typeof(WTextRange) && !((WTextRange)textRange.PreviousSibling).CharacterFormat.Italic))
                {
                    textRange.Text = textRange.Text.Replace(textRange.Text, $"_{textRange.Text}");
                }
                if (textRange.NextSibling == null || (textRange.NextSibling.GetType() == typeof(WTextRange) && !((WTextRange)textRange.NextSibling).CharacterFormat.Italic))
                {
                    textRange.Text = textRange.Text.Replace(textRange.Text, $"{textRange.Text}_");
                }
            }
            if (textRange.CharacterFormat.Strikeout)
            {
                if (textRange.PreviousSibling == null || (textRange.PreviousSibling.GetType() == typeof(WTextRange) && !((WTextRange)textRange.PreviousSibling).CharacterFormat.Strikeout))
                {
                    textRange.Text = textRange.Text.Replace(textRange.Text, $"-{textRange.Text}");
                }
                if (textRange.NextSibling == null || (textRange.NextSibling.GetType() == typeof(WTextRange) && !((WTextRange)textRange.NextSibling).CharacterFormat.Strikeout))
                {
                    textRange.Text = textRange.Text.Replace(textRange.Text, $"{textRange.Text}-");
                }
            }
            if (textRange.CharacterFormat.UnderlineStyle != UnderlineStyle.None)
            {
                if (textRange.PreviousSibling == null || (textRange.PreviousSibling.GetType() == typeof(WTextRange) && ((WTextRange)textRange.PreviousSibling).CharacterFormat.UnderlineStyle != UnderlineStyle.Single))
                {
                    textRange.Text = textRange.Text.Replace(textRange.Text, $"+{textRange.Text}");
                }
                if (textRange.NextSibling == null || (textRange.NextSibling.GetType() == typeof(WTextRange) && ((WTextRange)textRange.NextSibling).CharacterFormat.UnderlineStyle != UnderlineStyle.Single))
                {
                    textRange.Text = textRange.Text.Replace(textRange.Text, $"{textRange.Text}+");
                }
            }

            

            return textRange;
        }

        // Saves the current state inside a memento.
        public IMemento Save()
        {
            return new DescriptionModel(this._state);
        }

        // Restores the Originator's state from a memento object.
        public void Restore(IMemento memento)
        {
            if (!(memento is DescriptionModel))
            {
                throw new Exception("Unknown memento class " + memento.ToString());
            }

            this._state = (DescriptionModel)memento.GetState();
            log.Debug($"DescriptionParser: My state has changed to: {JsonSerializer.Serialize(this._state)}");
        }
    }
}