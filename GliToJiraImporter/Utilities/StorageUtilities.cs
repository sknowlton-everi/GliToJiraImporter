using Atlassian.Jira;
using Azure;
using CsvHelper.Configuration;
using GliToJiraImporter.Models;
using log4net;
using Newtonsoft.Json;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using Issue = Atlassian.Jira.Issue;

namespace GliToJiraImporter.Utilities
{
    public class StorageUtilities
    {
        //TODO Cleanup
        //private const string CLAUSE_ID = "GLIClauseId";
        //private const string CATEGORY = "GLICategory";
        //private const string SUBCATEGORY = "GLISubCategory";
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        //TODO Cleanup
        //private readonly Jira jiraConnection;
        private ParameterModel parameterModel;
        private JiraRequestUtilities jiraRequestUtilities = new JiraRequestUtilities();

        //TODO Cleanup
        public StorageUtilities(ParameterModel parameterModel)//, Jira jiraConnection)
        {
            this.parameterModel = parameterModel;
            //TODO Cleanup
            //this.jiraConnection = jiraConnection;
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
        public void SaveCsv(string fileName, IList<CategoryModel> categoryModelList)//TODO IList<IMemento> ?
        {
            // Category;SubCategory;ClauseID;Description;AttachmentList
            string csvHeaders = "Category,SubCategory,ClauseID,Description,AttachmentList";
            var csvConfig = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                HasHeaderRecord = true
            };

            string csvRegulations = "";
            foreach (CategoryModel categoryModel in categoryModelList)
            {
                foreach (RegulationModel regulationModel in categoryModel.RegulationList)
                {
                    csvRegulations += $"\"{categoryModel.Category}\",\"{regulationModel.Subcategory}\",\"{regulationModel.ClauseID.FullClauseId}\",\"{regulationModel.Description.GetName().Replace('\"', '\'')}\"";
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
                // Check if file already exists. If yes, delete it.
                if (File.Exists(fileName)) File.Delete(fileName);

                // Create a new file
                using (FileStream fs = File.Create(fileName))
                {
                    // Add some text to file
                    Byte[] textBytes = new UTF8Encoding(true).GetBytes(csvHeaders + "\n" + csvRegulations);
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
            foreach (CategoryModel categoryModel in categoryModelList)
            {
                foreach (RegulationModel regulationModel in categoryModel.RegulationList)
                {
                    if (jiraExistingClauseIdList.ContainsKey(regulationModel.ClauseID.GetName()))
                    {
                        log.Debug($"Skipping clauseId {regulationModel.ClauseID.FullClauseId} because it already exists in the project {parameterModel.ProjectKey}");
                        log.Debug($"{categoryModel.RegulationList.IndexOf(regulationModel) + 1}/{categoryModel.RegulationList.Count} Complete processing.");
                        continue;
                    }
                    this.createIssue(regulationModel, categoryModel.Category);


                    log.Debug($"Completed issue creation attempt for GLI ClauseID {regulationModel.ClauseID.FullClauseId}");
                    log.Debug($"{categoryModel.RegulationList.IndexOf(regulationModel) + 1}/{categoryModel.RegulationList.Count} Complete processing.");

                    Thread.Sleep(this.parameterModel.SleepTime);
                }
            }
            log.Debug("Regulations upload ended");
        }

        private bool isExpectedIMementoType(IMemento memento, Type expectedType)
        {
            return typeof(IMemento).Equals(expectedType);
        }

        private void createIssue(RegulationModel regulationModel, string categoryName)
        {
            log.Debug("Creating Issue");
            JiraIssue jiraIssue = new JiraIssue(this.parameterModel.ProjectKey, "Test Plan", regulationModel.ClauseID.FullClauseId, regulationModel.ClauseID.FullClauseId, categoryName,
                regulationModel.Subcategory, regulationModel.Description.Text);
            //string jsonData = "{\"fields\": {\"project\": {\"key\": \"" + parameterModel.ProjectKey + "\"},";
            //jsonData += "\"issuetype\": {\"name\": \"" + parameterModel.IssueType + "\"},";
            //jsonData += "\"summary\": \"" + regulationModel.ClauseID + "\",";
            //jsonData += "\"customfield_10046\": \"" + regulationModel.ClauseID + "\",";
            //jsonData += "\"customfield_10044\": \"" + categoryName + "\",";
            //jsonData += "\"customfield_10045\": \"" + regulationModel.Subcategory + "\",";
            //jsonData += "\"description\": \"" + regulationModel.Description + "\"";
            //jsonData += "}}";
            //jsonData = jsonData.Replace('\'', '"');
            //log.Debug(jsonData);
            ////Models.Issue issue = new Models.Issue();
            ////issue.fields.issuetype = parameterModel.IssueType;
            ////issue.fields.summary = $"{regulationModel.ClauseID}";
            ////issue.fields.customfield_10046 = regulationModel.ClauseID.ToString();
            ////issue.fields.customfield_10044 = categoryName;
            ////issue.fields.customfield_10045 = regulationModel.Subcategory;
            ////issue.fields.description = $"{regulationModel.Description}";
            bool status = this.jiraRequestUtilities.PostIssue(jiraIssue, string.Empty);
            string appDataPath = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            if (status)
            {
                //for (int i = 0; i < regulationModel.AttachmentList.Count && status; i++)
                //{
                //    if (regulationModel.AttachmentList[i].ImageName == string.Empty)
                //    {
                //        regulationModel.AttachmentList[i].ImageName = $"{regulationModel.ClauseID.BaseClauseId}-attachment-#{i}.png";
                //          issue.AddAttachment(attachmentName, attachmentList[i].ImageBytes);
                //          issue.Description.Replace("(Image included below, Name: )", $"(Image included below, Name: {attachmentName})");
                //    }
                //    else
                //    {
                //        regulationModel.AttachmentList[i].ImageName = $"{regulationModel.AttachmentList[i].ImageName}.png";
                //    }
            //    regulationModel.AttachmentList[i].ImageName = regulationModel.AttachmentList[i].ImageName.Replace(" ", "-");

            //    File.Create(appDataPath + @"\TempImages\" + regulationModel.AttachmentList[i].ImageName).Close();
            //    File.WriteAllBytes(appDataPath + @"\TempImages\" + regulationModel.AttachmentList[i].ImageName, regulationModel.AttachmentList[i].ImageBytes);
            //    status = this.jiraRequestUtilities.PutIssueByKey(jiraIssue, appDataPath + @"\TempImages\" + regulationModel.AttachmentList[i].ImageName, jiraIssue.fields.GliClauseId);
            //    File.Delete(appDataPath + @"\TempImages\" + regulationModel.AttachmentList[i].ImageName);
            //}
            //if (!status)
            //{
            //    log.Error("Attachments could not be added.");
            //}
        }
            else
            {
                log.Error("Issue could not be created.");
            }
        }

        private IDictionary<string, string> getExistingClauseIds()
        {
            IDictionary<string, string> existingClauseIdList = new Dictionary<string, string>();

            // build out our list of GLIClauseIds - this will prevent us from duplicating already added GLI requirements when someone re-runs the tool
            //int index = 0;
            //int itemsPerPage = 50;
            //string queryString = string.Format("project = {0}", parameterModel.ProjectKey);
            //IPagedQueryResult<Issue> jiraExistingIssueList = this.jiraConnection.Issues.GetIssuesFromJqlAsync(queryString, itemsPerPage, index).Result;
            IList<Models.Issue> jiraExistingIssueList = this.jiraRequestUtilities.GetAllIssuesWithAClauseId();
            //while (index < jiraExistingIssueList.TotalItems)
            //{
            //    index += itemsPerPage;
            foreach (GliToJiraImporter.Models.Issue issue in jiraExistingIssueList)
            {
                if (issue.fields.customfield_10000 != null)
                {
                    existingClauseIdList.Add((string)issue.fields.customfield_10046, issue.id);
                }
            }
            //jiraExistingIssueList = jiraConnection.Issues.GetIssuesFromJqlAsync(queryString, itemsPerPage, index).Result;
            //}

            return existingClauseIdList;
        }
    }
}