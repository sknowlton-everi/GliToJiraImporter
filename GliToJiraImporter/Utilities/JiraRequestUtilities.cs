using GliToJiraImporter.Models;
using log4net;
using Newtonsoft.Json;
using RestSharp;
using System.Net;
using System.Reflection;
using System.Text;

namespace GliToJiraImporter.Utilities
{
    public class JiraRequestUtilities
    {
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private ParameterModel parameterModel = new ParameterModel();

        public JiraRequestUtilities() { }

        public JiraRequestUtilities(ParameterModel parameterModel)
        {
            this.parameterModel = parameterModel;
        }

        // Create
        public bool PostIssue(JiraIssue jiraIssue)
        {
            bool requestSucceeded = false;

            string requestUri = $"{parameterModel.JiraUrl}/issue";

            RestRequest request = createRestRequest(Method.POST, requestUri, jiraIssue);
            if (request == null)
            {
                log.Error("Failed to create request. Result returned as null.");
                return false;
            }

            object response = this.runQuery(request, requestUri);

            if (response.GetType().Equals(typeof(bool)))
            {
                requestSucceeded = (bool)response;
            }
            else if (response.GetType().Equals(typeof(ErrorRoot)))
            {
                requestSucceeded = false;
            }

            return requestSucceeded;
        }

        // Update
        public bool PostAttachmentToIssueByKey(JiraIssue jiraIssue, string attachmentPath, string issueKey)
        {
            bool requestSucceeded = false;

            string requestUri = $"{parameterModel.JiraUrl}/issue/{issueKey}";
            if (!attachmentPath.Equals(string.Empty))
            {
                requestUri += "/attachments";
            }

            RestRequest request = createRestRequest(Method.POST, requestUri, jiraIssue, attachmentPath);
            if (request == null)
            {
                log.Error("Failed to create request. Result returned as null.");
                return false;
            }

            object response = this.runQuery(request, requestUri);

            if (response.GetType().Equals(typeof(bool)))
            {
                requestSucceeded = (bool)response;
            }
            else if (response.GetType().Equals(typeof(ErrorRoot)) || response.GetType().Equals(typeof(string)))
            {
                requestSucceeded = false;
            }

            return requestSucceeded;
        }


        // Read
        public IList<Models.Issue> GetAllIssuesWithAClauseId()
        {
            List<Models.Issue> result = new();

            string requestUri = $"{parameterModel.JiraUrl}/search";

            RestRequest request = createRestRequest(Method.GET, requestUri);
            if (request == null)
            {
                log.Error("Failed to create request. Result returned as null.");
                return null;
            }

            object response = this.runQuery(request, requestUri);

            Type responseType = response.GetType();
            if (response.GetType().Equals(typeof(List<Models.Issue>)))
            {
                foreach (Models.Issue issue in (List<Models.Issue>)response)
                {
                    if (issue.fields.customfield_10046 != null)
                    {
                        result.Add(issue);
                    }
                }
            }
            else if (response.GetType().Equals(typeof(ErrorRoot)))
            {
                result = null;
            }
            else
            {
                log.Debug("No issues found.");
            }

            return result;
        }

        public Models.Issue GetIssueByClauseId(string clauseId)
        {
            Models.Issue result = new();

            string requestUri = $"{parameterModel.JiraUrl}/search?jql=GLIClauseId~'{clauseId}'";

            RestRequest request = createRestRequest(Method.GET, requestUri);
            if (request == null)
            {
                log.Error("Failed to create request. Result returned as null.");
                return null;
            }

            object response = this.runQuery(request, requestUri);

            if (response.GetType().Equals(typeof(List<Models.Issue>)))
            {
                List<Models.Issue> matchingIssues = (List<Models.Issue>)response;
                foreach (Issue issue in matchingIssues)
                {
                    if (issue.fields.customfield_10046.Equals(clauseId))
                    {
                        result = this.GetIssueByIssueKey(((Models.Issue)response).key);
                    }
                }
            }
            else if (response.GetType().Equals(typeof(Models.Issue)))
            {
                Models.Issue matchingIssue = (Models.Issue)response;
                if (matchingIssue.fields.customfield_10046.Equals(clauseId))
                {
                    result = this.GetIssueByIssueKey(((Models.Issue)response).key);
                }
            }
            else if (response.GetType().Equals(typeof(ErrorRoot)))
            {
                result = null;
            }
            else
            {
                log.Debug("No issues found.");
            }

            return result;
        }

        public Models.Issue GetIssueByIssueKey(string issueKey)
        {
            Models.Issue result = new Models.Issue();

            string requestUri = $"{parameterModel.JiraUrl}/issue/{issueKey}";

            RestRequest request = createRestRequest(Method.GET, requestUri);
            if (request == null)
            {
                log.Error("Failed to create request. Result returned as null.");
                return null;
            }

            object response = this.runQuery(request, requestUri);

            if (response.GetType().Equals(typeof(Models.Issue)))
            {
                result = (Models.Issue)response;
            }
            else if (response.GetType().Equals(typeof(ErrorRoot)))
            {
                result = null;
            }
            else
            {
                log.Debug("No issues found.");
            }

            return result;
        }

        public byte[] GetIssueAttachmentById(string attachmentId)
        {
            byte[] result = {};

            string requestUri = $"{parameterModel.JiraUrl}/attachment/content/{attachmentId}";

            RestRequest request = createRestRequest(Method.GET, requestUri);
            if (request == null)
            {
                log.Error("Failed to create request. Result returned as null.");
                return null;
            }

            object response = this.runQuery(request, requestUri);

            if (response.GetType().Equals(typeof(byte[])))
            {
                result = (byte[])response;
            }
            else if (response.GetType().Equals(typeof(ErrorRoot)))
            {
                result = null;
            }
            else
            {
                log.Debug("No issues found.");
            }

            return result;
        }

        // Delete
        public bool DeleteIssueByKey(string issueKey)
        {
            bool requestSucceeded = false;

            string requestUri = $"{parameterModel.JiraUrl}/issue/{issueKey}";

            RestRequest request = createRestRequest(Method.DELETE, requestUri);
            if (request == null)
            {
                log.Error("Failed to create request. Result returned as null.");
                return false;
            }

            object response = this.runQuery(request, requestUri);

            if (response.GetType().Equals(typeof(bool)))
            {
                requestSucceeded = (bool)response;
            }
            else if (response.GetType().Equals(typeof(ErrorRoot)) || response.GetType().Equals(typeof(string)))
            {
                requestSucceeded = false;
            }

            return requestSucceeded;
        }

        private RestRequest createRestRequest(Method method, string requestUri)
        {
            return this.createRestRequest(method, requestUri, new JiraIssue());
        }

        private RestRequest createRestRequest(Method method, string requestUri, JiraIssue jiraIssue)
        {
            return this.createRestRequest(method, requestUri, jiraIssue, string.Empty);
        }

        private RestRequest createRestRequest(Method method, string requestUri, JiraIssue jiraIssue, string filePath)
        {
            RestRequest result = new(requestUri, method);

            if (requestUri.Equals(string.Empty))
            {
                log.Error("Request URI was empty.");
                return null;
            }

            string base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes(parameterModel.UserName));

            if (result.AddHeader("Authorization", $"Basic {base64authorization}") == null)
            {
                log.Error("Authorization header could not be added.");
                return null;
            }

            if (method.Equals(Method.POST))
            {
                if (!filePath.Equals(string.Empty))
                {
                    result.AddHeader("X-Atlassian-Token", "no-check");
                    result.AddFile("file", filePath);
                }
                else if (filePath.Equals(string.Empty) && (jiraIssue.fields.project.key == null || jiraIssue.fields.project.key.Equals(string.Empty)))
                {
                    log.Error("Issue project key was null or empty.");
                    return null;
                }
                else
                {
                    result.AddJsonBody(jiraIssue, "application/json");
                    result.AddHeader("Content-Type", "application/json");
                }
            }
            else if (method.Equals(Method.GET))
            {
                result.AddHeader("X-Atlassian-Token", "no-check");
            }

            return result;
        }

        //TODO cleanup
        private object runQuery(RestRequest request, string requestUri)
        {
            object result = new();
            try
            {
                RestClient client = new RestClient();

                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.NoContent)
                {
                    log.Debug($"Request succeeded. Request: {requestUri}, Status: {response.StatusCode}");
                    string jsonString = response.Content;
                    if (request.Method == Method.GET)
                    {

                        if (requestUri.EndsWith("/search") || requestUri.Contains("issue"))
                        {
                            result = JsonConvert.DeserializeObject<Models.Root>(jsonString).issues;
                            if (result == null)
                            {
                                result = JsonConvert.DeserializeObject<Models.Issue>(jsonString);
                            }
                        }
                        else if (requestUri.Contains("attachment/content"))
                        {
                            result = client.DownloadData(request);
                        }
                        else
                        {
                            result = JsonConvert.DeserializeObject<Models.Root>(jsonString).issues.First();
                        }
                    }
                    else if (request.Method == Method.POST || request.Method == Method.DELETE || request.Method == Method.PUT)
                    {
                        result = true;
                    }
                }
                else
                {
                    string jsonString = response.Content;
                    try
                    {
                        ErrorRoot jsonObject = JsonConvert.DeserializeObject<ErrorRoot>(jsonString);
                        string errorMsg = $"Request failed. Request: {requestUri}, Status: {response.StatusCode}, ErrorMessages: ";
                        jsonObject.ErrorMessages.ForEach(err => errorMsg += err + ",");
                        log.Debug(errorMsg);
                        result = jsonObject;
                    }
                    catch (NullReferenceException)
                    {
                        log.Debug($"Request failed. Request: {requestUri}, Status: {response.StatusCode}, ErrorMessages: {jsonString}");
                        result = jsonString;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            return result;
        }
    }
}
