using GliToJiraImporter.Models;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
        public bool PostIssue(JiraIssue jiraIssue, string attachmentPath)
        {
            bool requestSucceeded = false;

            string urlRequest = $"{parameterModel.JiraUrl}/issue";
            //if (!attachmentPath.Equals(string.Empty))
            //{
            //    urlRequest = "-F \"file=@" + attachmentPath + "\" " + urlRequest;
            //}

            object response = this.runQuery(HttpMethod.Post, urlRequest, jiraIssue, attachmentPath);
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

        // Read
        public IList<Models.Issue> GetAllIssuesWithAClauseId()
        {
            List<Models.Issue> result = new List<Models.Issue>();

            string urlRequest = $"{parameterModel.JiraUrl}/search";
            object response = this.runQuery(HttpMethod.Get, urlRequest, new(), string.Empty);
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

            //        if (response.StatusCode == HttpStatusCode.OK)
            //        {
            //            string jsonString = response.Content.ReadAsStringAsync().Result;
            //            result = JsonConvert.DeserializeObject<List<Models.Issue>>(jsonString);
            //            log.Debug($"Request succeeded. Request: {urlRequest}, Status: {response.StatusCode}");
            //            result.ForEach(issue => log.Debug(issue.ToString()));
            //        }
            //        else
            //        {
            //            string jsonString = response.Content.ReadAsStringAsync().Result;
            //            ErrorRoot jsonObject = JsonConvert.DeserializeObject<ErrorRoot>(jsonString);
            //            log.Debug($"Request failed. Request: {urlRequest}, Status: {response.StatusCode}, ErrorMessages: {jsonObject.errorMessages}");
            //            //log.Debug(jsonObject.errorMessages);
            //        }

            ////        IList<GliToJiraImporter.Models.Issue> jiraExistingIssueList =
            ////JsonConvert.DeserializeObject<List<Models.Issue>>();

            return result;
        }

        // Update
        public bool PutIssueByKey(JiraIssue jiraIssue, string attachmentPath, string issueKey)
        {
            bool requestSucceeded = false;

            string urlRequest = $"{parameterModel.JiraUrl}/issue/{issueKey}";
            //if (!attachmentPath.Equals(string.Empty))
            //{
            //    urlRequest = "-F \"file=@" + attachmentPath + "\" " + urlRequest;
            //}

            object response = this.runQuery(HttpMethod.Put, urlRequest, jiraIssue, attachmentPath);
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

        // Delete
        public bool DeleteIssueByKey(string issueKey)
        {
            bool requestSucceeded = false;

            string urlRequest = $"{parameterModel.JiraUrl}/issue/{issueKey}";

            object response = this.runQuery(HttpMethod.Delete, urlRequest, new(), string.Empty);
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

        private object runQuery(HttpMethod method, string urlRequest, JiraIssue jiraIssue, string file)
        {
            object result = new();
            try
            {
                using HttpClient httpClient = new();
                using HttpRequestMessage request = new(method, urlRequest);
                string base64authorization =
                    Convert.ToBase64String(Encoding.ASCII.GetBytes(parameterModel.UserName));
                request.Headers.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");
                //request.Headers.TryAddWithoutValidation()
                if (jiraIssue.fields.Project.Key != null && !jiraIssue.fields.Project.Key.Equals(string.Empty))
                {
                    //request.Content = new StringContent(body);
                    JsonContent jc = JsonContent.Create(jiraIssue, MediaTypeHeaderValue.Parse("application/json"));
                    request.Content = jc;
                    log.Debug(JsonConvert.SerializeObject(jiraIssue));
                    //System.Net.Http.Formatting.MediaTypeFormatter jsonFormatter = new System.Net.Http.Formatting.JsonMediaTypeFormatter();

                    //System.Net.Http.HttpContent content = new System.Net.Http.ObjectContent<string>(data, jsonFormatter);
                    //System.Net.Http.HttpContent content = new System.Net.Http.ObjectContent<Models.Issue>(body, jsonFormatter);
                    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    request.Headers.TryAddWithoutValidation("Content-Type", "application/json");
                }

                if (!file.Equals(string.Empty))
                {
                    //request.Properties.TryAdd("file");
                    request.Content = new MultipartContent();
                    request.Content.Headers.TryAddWithoutValidation("file", file);
                }



                //System.Net.Http.HttpResponseMessage response = client.PostAsync(urlRequest, content).Result;





                HttpResponseMessage response = httpClient.Send(request);
                //HttpResponseMessage response = httpClient.PostAsync(urlRequest, request.Content).Result;//.Send(request);
                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.NoContent)
                {
                    log.Debug($"Request succeeded. Request: {urlRequest}, Status: {response.StatusCode}");
                    string jsonString = response.Content.ReadAsStringAsync().Result;
                    if (method == HttpMethod.Get && urlRequest.Contains("/search"))
                    {
                        result = JsonConvert.DeserializeObject<Models.Root>(jsonString).issues;
                    }
                    else if (method == HttpMethod.Get)
                    {
                        result = JsonConvert.DeserializeObject<Models.Issue>(jsonString);
                    }
                    else if (method == HttpMethod.Post || method == HttpMethod.Delete || method == HttpMethod.Put)
                    {
                        result = true;
                    }
                }
                else
                {
                    string jsonString = response.Content.ReadAsStringAsync().Result;
                    try
                    {
                        ErrorRoot jsonObject = JsonConvert.DeserializeObject<ErrorRoot>(jsonString);
                        string errorMsg = $"Request failed. Request: {urlRequest}, Status: {response.StatusCode}, ErrorMessages: ";
                        jsonObject.errorMessages.ForEach(err => errorMsg += err + ",");
                        log.Debug(errorMsg);
                        result = jsonObject;
                    }
                    catch (NullReferenceException)
                    {
                        log.Debug($"Request failed. Request: {urlRequest}, Status: {response.StatusCode}, ErrorMessages: {jsonString}");
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
