﻿using GliToJiraImporter.Models;
using GliToJiraImporter.Types;
using log4net;
using Syncfusion.DocIO.DLS;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;

namespace GliToJiraImporter.Parsers
{
    public class Parser
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        private readonly ParameterModel parameterModel;

        public Parser(ParameterModel parameterModel)
        {
            log.Info(new StackFrame().GetMethod()?.Name);
            this.parameterModel = parameterModel;
        }

        public IList<CategoryModel> Parse()
        {
            log.Debug("Checking doc type...");
            IList<CategoryModel> result;
            if (this.parameterModel.Type == (int)DocumentType.CHECK_OFF)
            {
                log.Debug($"Document Type: {this.parameterModel.GetType().Name}");
                log.Debug("Parsing...");
                result = this.parseMementos();
            }
            else
            {
                log.Debug($"Document Type Unknown: {this.parameterModel.Type}");
                // Log and throw unknown type
                throw new Exception("Provided Document Type is Unknown. Try type 1 for Checkoff documents.");//TODO is this okay? I don't like the vagueness of type 'Exception'
            }
            log.Debug($"Completed list size: {result.Count}");

            return result;
        }

        private IList<CategoryModel> parseMementos()
        {
            IList<CategoryModel> result = new List<CategoryModel>();
            IDictionary<string, Color> regulationDocs = new Dictionary<string, Color>();

            // Originator and Caretaker instantiation
            CategoryParser categoryOriginator = new();
            Caretaker caretaker = new(categoryOriginator);

            // Creates an instance of WordDocument class
            WSection section = this.getDocumentFromPath().Sections[0];

            // Iterates the tables of the section
            int i = 4;
            //for (; i < section.Tables.Count; i++)
            //{
            //    WTableCell firstCell = section.Tables[i].Rows[0].Cells[0];
            //    if (firstCell.Paragraphs[0].Text.Contains("Tested against Requirements"))
            //    {
            //        regulationDocs = this.parseDocumentColors(section.Tables[i]);
            //        break;
            //    }
            //}

            // Continues iterating the tables of the section
            //TODO if i is set to anything below 4, the tests get the following error. When running it myself it seems to maybe be an infinite loop issue
            //  log4net:ERROR RollingFileAppender: INTERNAL ERROR. Append is False but OutputFile [C:\MForce\GliToJiraImporter\GliToJiraImporter.Testing\bin\Debug\net6.0\gliToJiraImporter.log] already exists.
            for (; i < section.Tables.Count; i++)
            {
                // Iterates the rows of the table
                for (int j = 0; j < section.Tables[i].Rows.Count; j++)
                {
                    // Backup state and parse
                    log.Debug("Backing up and parsing...");
                    caretaker.Backup();
                    bool isCategoryComplete = categoryOriginator.Parse(section.Tables[i], ref j);

                    if (!isCategoryComplete) continue;

                    j--;
                    // Validate the parse and undo if invalid
                    if (!categoryOriginator.Save().IsValid())
                    {
                        log.Debug("Category Parsing invalid, undoing");
                        caretaker.Undo();
                    }
                    else if (categoryOriginator.Save().IsValid())
                    {
                        log.Debug("Category Parsing valid");
                        result.Add((CategoryModel)categoryOriginator.Save());
                        categoryOriginator = new CategoryParser();
                    }
                }
            }

            if (categoryOriginator.Save().IsValid())
            {
                result.Add((CategoryModel)categoryOriginator.Save());
            }
            else
            {
                CategoryModel x = (CategoryModel)categoryOriginator.Save();

                if (!x.RegulationList.Any()) return result;

                x.RegulationList.RemoveAt(x.RegulationList.Count - 1);
                if (x.IsValid())
                {
                    result.Add(x);
                }
            }
            return result;
        }

        private WordDocument getDocumentFromPath()
        {
            using FileStream fs = File.Open(this.parameterModel.FilePath, FileMode.Open);
            return new WordDocument(fs);
        }

        private IDictionary<string, Color> parseDocumentColors(IWTable table)
        {
            IDictionary<string, Color> result = new Dictionary<string, Color>();
            for (int j = 0; j < table.Rows.Count; j++)
            {
                for (int k = 1; k < table.Rows[j].Cells.Count; k++)
                {
                    WParagraph paragraph = table.Rows[j].Cells[k].Paragraphs[0];
                    for (int l = 0; l < paragraph.Items.Count; l++)
                    {
                        result.Add(paragraph.Text, ((WTextRange)paragraph.Items[l]).CharacterFormat.TextColor);
                    }
                }
            }

            return result;
        }
    }
}