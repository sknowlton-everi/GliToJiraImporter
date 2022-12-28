using CsvHelper.Configuration;
using GliToJiraImporter.Models;
using log4net;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace GliToJiraImporter.Utilities
{
    public class StorageUtilities
    {
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        private readonly ParameterModel parameterModel = new();
        private readonly JiraRequestUtilities jiraRequestUtilities = new();

        public StorageUtilities() { }

        public StorageUtilities(ParameterModel parameterModel)
        {
            this.parameterModel = parameterModel;
            this.jiraRequestUtilities = new JiraRequestUtilities(parameterModel);
        }

        public void SaveText(string fileName, string text)
        {
            try
            {
                // Check if file already exists. If yes, delete it.
                if (File.Exists(fileName)) File.Delete(fileName);

                // Create a new file
                using (FileStream fs = File.Create(fileName))
                {
                    // Add some text to file
                    Byte[] textBytes = new UTF8Encoding(true).GetBytes(text);
                    fs.Write(textBytes, 0, textBytes.Length);
                }
            }
            catch (Exception Ex)
            {
                log.Error(Ex.ToString());
            }
        }

        // This doesn't fully work
        public void SaveCsv(string fileFolder, IList<CategoryModel> categoryModelList)//TODO IList<IMemento> ?
        {
            // Category;SubCategory;ClauseID;Description;AttachmentList
            string csvHeaders = "Category,SubCategory,ClauseID,Description,AttachmentList";
            CsvConfiguration csvConfig = new(CultureInfo.CurrentCulture)
            {
                HasHeaderRecord = true
            };

            string csvRegulations = "";
            foreach (CategoryModel categoryModel in categoryModelList)
            {
                foreach (RegulationModel regulationModel in categoryModel.RegulationList)
                {
                    csvRegulations += $"\"{categoryModel.Category}\",\"{regulationModel.Subcategory}\",\"{regulationModel.ClauseId.FullClauseId}\",\"{regulationModel.Description.Text.Replace('\"', '\'')}\"";
                    //csvWriter.WriteRecord($"{categoryModel.Category},{regulationModel.Subcategory},{regulationModel.ClauseID},{regulationModel.Description},");
                    //foreach (byte[] image in regulationModel.AttachmentList)
                    //{
                    //    csvRegulations += ",{}";
                    //    csvWriter.WriteField(image);
                    //    csvWriter.WriteRecord(";");
                    //}
                    //csvWriter.NextRecord();
                    csvRegulations += "\n";
                }
            }

            try
            {
                string fileName = $"{fileFolder}\\ResultsCsv.csv";

                // Check if file already exists. If yes, delete it.
                if (File.Exists(fileName)) File.Delete(fileName);

                // Create a new file
                using (FileStream fs = File.Create(fileName))
                {
                    // Add some text to file
                    byte[] textBytes = new UTF8Encoding(true).GetBytes(csvHeaders + "\n" + csvRegulations);
                    fs.Write(textBytes, 0, textBytes.Length);
                }
            }
            catch (Exception Ex)
            {
                log.Error(Ex.ToString());
            }
        }

        public void UploadToJira(IList<IMemento> categoryModelList)
        {
            for (int i = 0; i < categoryModelList.Count; i++)
            {
                if (this.isExpectedIMementoType(categoryModelList[i], typeof(CategoryModel)))
                {
                    CategoryModel categoryModel = (CategoryModel)categoryModelList[i];
                    for (int j = 0; j < categoryModel.RegulationList.Count; j++)
                    {
                        //if (isExpectedIMementoType(categoryModel.RegulationList[j], typeof(RegulationModel)))
                        //{
                        //TODO Add the above if-statement, if we change the list type of RegulationList from "IList<RegulationModel>" to "IList<IMemento>"
                        RegulationModel regulationModel = (RegulationModel)categoryModel.RegulationList[j];
                        this.createIssue(regulationModel, categoryModel.Category);
                        //}
                    }
                }
                else
                {
                    log.Error($"Model at index #{i} is not of type CategoryModel.", new DataMisalignedException());
                    //TODO Is this an okay exception? (DataMisalignedException)
                }
            }
        }

        public void UploadToJira(IList<CategoryModel> categoryModelList)
        {
            log.Debug("Starting regulations upload to Jira");
            IDictionary<string, string> jiraExistingClauseIdList = this.getExistingClauseIds();
            if (jiraExistingClauseIdList == null)
            {
                log.Error("this.getExistingClauseIds returned null, and therefore failed");
                return;
            }

            foreach (CategoryModel categoryModel in categoryModelList)
            {
                foreach (RegulationModel regulationModel in categoryModel.RegulationList)
                {
                    if (jiraExistingClauseIdList.ContainsKey(regulationModel.ClauseId.FullClauseId))
                    {
                        log.Debug($"Skipping clauseId {regulationModel.ClauseId.FullClauseId} because it already exists in the project {parameterModel.ProjectKey}");
                        log.Debug($"{categoryModel.RegulationList.IndexOf(regulationModel) + 1}/{categoryModel.RegulationList.Count} Complete processing.");
                        continue;
                    }
                    this.createIssue(regulationModel, categoryModel.Category);


                    log.Debug($"Completed issue creation attempt for GLI ClauseID {regulationModel.ClauseId.FullClauseId}");
                    log.Debug($"{categoryModel.RegulationList.IndexOf(regulationModel) + 1}/{categoryModel.RegulationList.Count} Complete processing.");

                    Thread.Sleep(this.parameterModel.SleepTime);
                }
            }
            log.Debug("Regulations upload ended");
        }

        private bool isExpectedIMementoType(IMemento memento, Type expectedType)
        {
            return typeof(IMemento) == expectedType;
        }

        private void createIssue(RegulationModel regulationModel, string categoryName)
        {
            log.Debug("Creating Issue");
            JiraIssue jiraIssue = new(this.parameterModel.ProjectKey, "Test Plan", regulationModel.ClauseId.FullClauseId, regulationModel.ClauseId.FullClauseId, categoryName,
                regulationModel.Subcategory, regulationModel.Description.Text);

            bool status = this.jiraRequestUtilities.PostIssue(jiraIssue);
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            if (status && regulationModel.Description.AttachmentList.Count != 0)
            {
                string issueKey = this.jiraRequestUtilities.GetIssueByClauseId(jiraIssue.fields.customfield_10046).key;
                for (int i = 0; i < regulationModel.Description.AttachmentList.Count && status; i++)
                {
                    if (regulationModel.Description.AttachmentList[i].ImageName == string.Empty)
                    {
                        regulationModel.Description.AttachmentList[i].ImageName = $"{regulationModel.ClauseId.BaseClauseId}-attachment-#{i}.png";
                        jiraIssue.fields.description.Replace("(Image included below, Name: )", $"(Image included below, Name: {regulationModel.Description.AttachmentList[i].ImageName})");
                    }
                    regulationModel.Description.AttachmentList[i].ImageName = regulationModel.Description.AttachmentList[i].ImageName.Replace(" ", "-");

                    File.Create(appDataPath + @"\TempImages\" + regulationModel.Description.AttachmentList[i].ImageName).Close();
                    File.WriteAllBytes(appDataPath + @"\TempImages\" + regulationModel.Description.AttachmentList[i].ImageName, regulationModel.Description.AttachmentList[i].ImageBytes);
                    status = this.jiraRequestUtilities.PostAttachmentToIssueByKey(jiraIssue, appDataPath + @"\TempImages\" + regulationModel.Description.AttachmentList[i].ImageName, issueKey);
                    File.Delete(appDataPath + @"\TempImages\" + regulationModel.Description.AttachmentList[i].ImageName);
                }
                if (!status)
                {
                    log.Error("Attachments could not be added.");
                }
            }
            else if (!status)
            {
                log.Error("Issue could not be created.");
            }
        }

        private IDictionary<string, string> getExistingClauseIds()
        {
            IDictionary<string, string> existingClauseIdList = new Dictionary<string, string>();

            // build out our list of GLIClauseIds - this will prevent us from duplicating already added GLI requirements when someone re-runs the tool
            IList<Models.Issue> jiraExistingIssueList = this.jiraRequestUtilities.GetAllIssuesWithAClauseId();
            if (jiraExistingIssueList == null)
            {
                log.Error("JiraRequestUtilities.GetAllIssuesWithAClauseId returned null, and therefore failed");
                return null;
            }

            foreach (Models.Issue issue in jiraExistingIssueList)
            {
                existingClauseIdList.Add((string)issue.fields.customfield_10046, issue.id);
            }

            return existingClauseIdList;
        }

        public bool VerifyCategoryModelsExistInJira(IList<CategoryModel> categoryModels)
        {
            bool result = true;
            const string errorMessage = "Regulation {0} was not uploaded correctly.";
            IList<Issue> jiraIssues = this.jiraRequestUtilities.GetAllIssuesWithAClauseId();

            for (int i = 0; i < categoryModels.Count; i++)
            {

                for (int j = 0; j < categoryModels[i].RegulationList.Count; j++)
                {
                    Issue issue = this.jiraRequestUtilities.GetIssueByClauseId(categoryModels[i].RegulationList[j].ClauseId.FullClauseId);
                    RegulationModel regulationModel = categoryModels[i].RegulationList[j];
                    if (issue.fields == null)
                    {
                        this.log.Error($"No issue with ClauseId {categoryModels[i].RegulationList[j].ClauseId.FullClauseId} was found.");
                        continue;
                    }

                    if (!regulationModel.ClauseId.FullClauseId.Equals(issue.fields.customfield_10046))
                    {
                        log.Error(string.Format(errorMessage + " The ClauseIds did not match.", regulationModel.ClauseId.FullClauseId));
                        result = false;
                        continue;
                    }

                    if (!categoryModels[i].Category.Equals(issue.fields.customfield_10044))
                    {
                        log.Error(string.Format(errorMessage + " The Categories did not match.", regulationModel.ClauseId.FullClauseId));
                        result = false;
                        continue;
                    }

                    if (regulationModel.Subcategory != string.Empty && !regulationModel.Subcategory.Equals(issue.fields.customfield_10045))
                    {
                        log.Error(string.Format(errorMessage + " The Subcategories did not match.", regulationModel.ClauseId.FullClauseId));
                        result = false;
                        continue;
                    }

                    if (!regulationModel.Description.Text.Equals(issue.fields.description))
                    {
                        log.Error(string.Format(errorMessage + " The Subcategories did not match.", regulationModel.ClauseId.FullClauseId));
                        result = false;
                        continue;
                    }

                    if (regulationModel.Description.AttachmentList.Count > 0)
                    {
                        if (issue.fields.attachment == null || !regulationModel.Description.AttachmentList.Count.Equals(issue.fields.attachment.Count))
                        {
                            log.Error(string.Format(errorMessage + " The number of attachments did not match.", regulationModel.ClauseId.FullClauseId));
                            result = false;
                            continue;
                        }

                        for (int k = 0; k < regulationModel.Description.AttachmentList.Count; k++)
                        {
                            if (!regulationModel.Description.AttachmentList[k].ImageName.Equals(issue.fields.attachment[k].filename))
                            {
                                log.Error(string.Format(errorMessage + " The attachments names did not match for attachment {1}.", regulationModel.ClauseId.FullClauseId, regulationModel.Description.AttachmentList[k].ImageName));
                                result = false;
                                continue;
                            }

                            byte[] attachment = jiraRequestUtilities.GetIssueAttachmentById(issue.fields.attachment[k].id);
                            if (!regulationModel.Description.AttachmentList[k].ImageBytes.SequenceEqual(attachment))
                            {
                                log.Error(string.Format(errorMessage + " The attachments bytes did not match for attachment {1}.", regulationModel.ClauseId.FullClauseId, regulationModel.Description.AttachmentList[k].ImageName));
                                result = false;
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}