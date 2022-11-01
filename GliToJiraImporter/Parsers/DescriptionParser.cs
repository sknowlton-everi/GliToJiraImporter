﻿using GliToJiraImporter.Models;
using GliToJiraImporter.Utilities;
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

            // Check for different text styling
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
                    textRange = this.ignoreUnintendedFormating(textRange);

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
                    if (textRange.CharacterFormat.UnderlineStyle == UnderlineStyle.Single)
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
                    result += textRange.Text;
                }
                else if (paragraph.Items[i].GetType() == typeof(WField) && ((WField)paragraph.Items[i]).FieldType == Syncfusion.DocIO.FieldType.FieldHyperlink)
                {
                    WField field = (WField)paragraph.Items[i];
                    string fieldValue = field.FieldValue.Replace("\"", "");
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
                        log.Info($"Link type is not accounted for");
                        result += $"{field.Text.Trim()}";
                    }

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
            }

            // Checking for what I guess are dashes, as Jira doesn't know how to read them
            result = result.Replace('\u001E', '-');

            this._state.State += result;
        }

        private WTextRange ignoreUnintendedFormating(WTextRange textRange)
        {
            string[] formatingChars = { @"\*", "_", "-", @"\+" };

            foreach (string character in formatingChars)
            {
                string formatingCharRegex = "([^A-Za-z0-9])(" + character + @")([^\s].+[^\s])(" + character + ")([^A-Za-z0-9])";
                textRange.Text = Regex.Replace(textRange.Text, formatingCharRegex, "$1\\$2$3\\$4$5");
            }

            return textRange;
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
