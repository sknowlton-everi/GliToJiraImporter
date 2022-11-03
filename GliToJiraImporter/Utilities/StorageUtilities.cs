using Atlassian.Jira;
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
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly Jira jiraConnection;
        private ParameterModel parameterModel;

        private const string CLAUSE_ID = "GLIClauseId";
        private const string CATEGORY = "GLICategory";
        private const string SUBCATEGORY = "GLISubCategory";

        public StorageUtilities(ParameterModel parameterModel, Jira jiraConnection)
        {
            this.parameterModel = parameterModel;
            this.jiraConnection = jiraConnection;
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
                    csvRegulations += $"\"{categoryModel.Category}\",\"{regulationModel.Subcategory}\",\"{regulationModel.ClauseID}\",\"{regulationModel.Description.Replace('\"', '\'')}\"";
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
                        Issue issue = this.createIssue(regulationModel, categoryModel.Category);
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
                    if (jiraExistingClauseIdList.ContainsKey(regulationModel.ClauseID))
                    {
                        log.Debug($"Skipping clauseId {regulationModel.ClauseID} because it already exists in the project {parameterModel.ProjectKey}");
                        log.Debug($"{categoryModel.RegulationList.IndexOf(regulationModel) + 1}/{categoryModel.RegulationList.Count} Complete processing.");
                        continue;
                    }
                    Issue issue = this.createIssue(regulationModel, categoryModel.Category);

                    log.Debug($"Created issue with GLI ClauseID {regulationModel.ClauseID}");
                    log.Debug($"{categoryModel.RegulationList.IndexOf(regulationModel) + 1}/{categoryModel.RegulationList.Count} Complete processing.");

                    Thread.Sleep(parameterModel.SleepTime);
                }
            }
            log.Debug("Regulations upload ended");
        }

        private bool isExpectedIMementoType(IMemento memento, Type expectedType)
        {
            return typeof(IMemento).Equals(expectedType);
        }

        private Issue createIssue(RegulationModel regulationModel, string categoryName)
        {
            log.Debug("Creating Issue");
            Issue issue = jiraConnection.CreateIssue(parameterModel.ProjectKey);
            issue.Type = parameterModel.IssueType;
            issue.Summary = $"{regulationModel.ClauseID}";
            issue[CLAUSE_ID] = regulationModel.ClauseID.ToString();
            issue[CATEGORY] = categoryName;
            issue[SUBCATEGORY] = regulationModel.Subcategory;
            issue.Description = $"{regulationModel.Description}";
            issue.SaveChanges();

            for (int i = 0; i < regulationModel.AttachmentList.Count; i++)
            {
                if (regulationModel.AttachmentList[i].ImageName == string.Empty)
                {
                    issue.AddAttachment($"{regulationModel.ClauseID} attachment #{i}.png", regulationModel.AttachmentList[i].ImageBytes);
                }
                else
                {
                    issue.AddAttachment($"{regulationModel.AttachmentList[i].ImageName}.png", regulationModel.AttachmentList[i].ImageBytes);
                }
            }
            issue.SaveChanges();

            return issue;
        }

        private IDictionary<string, string> getExistingClauseIds()
        {
            IDictionary<string, string> existingClauseIdList = new Dictionary<string, string>();

            // build out our list of GLIClauseIds - this will prevent us from duplicating already added GLI requirements when someone re-runs the tool
            int index = 0;
            int itemsPerPage = 50;
            string queryString = string.Format("project = {0}", parameterModel.ProjectKey);
            IPagedQueryResult<Issue> jiraExistingIssueList = this.jiraConnection.Issues.GetIssuesFromJqlAsync(queryString, itemsPerPage, index).Result;
            while (index < jiraExistingIssueList.TotalItems)
            {
                index += itemsPerPage;
                foreach (Issue issue in jiraExistingIssueList)
                {
                    if (issue[CLAUSE_ID] != null)
                    {
                        existingClauseIdList.Add(issue[CLAUSE_ID].Value, issue.JiraIdentifier);
                    }
                }
                jiraExistingIssueList = jiraConnection.Issues.GetIssuesFromJqlAsync(queryString, itemsPerPage, index).Result;
            }

            return existingClauseIdList;
        }
    }
}