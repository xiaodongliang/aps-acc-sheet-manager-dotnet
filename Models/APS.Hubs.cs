using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autodesk.Forge;
using Autodesk.Forge.Client;
using Autodesk.Forge.Model;
using Newtonsoft.Json;
using RestSharp;

class Sheet_Viewable
{
    public string urn { get; set; }
    public string guid { get; set; }
}
class Sheet
{

    public string type { get; set; }

    public string name { get; set; }

    public string number { get; set; }
    public string title { get; set; }
    public Sheet_Viewable viewable { get; set; }

}
class Sheets
{
    public List<Sheet> results { get; set; }

}
class Sheet_Version_Set
{
    public string type { get; set; }
    public string id { get; set; }
    public string name { get; set; }
}
class Sheet_Version_Sets
{
    public List<Sheet_Version_Set> results { get; set; }

}
public partial class APS
{
    const string APS_BASE_URL = "https://developer.api.autodesk.com";

    public async Task<IEnumerable<dynamic>> GetHubs(Tokens tokens)
    {
        var hubs = new List<dynamic>();
        var api = new HubsApi();
        api.Configuration.AccessToken = tokens.InternalToken;
        var response = await api.GetHubsAsync();
        foreach (KeyValuePair<string, dynamic> hub in new DynamicDictionaryItems(response.data))
        {
            hubs.Add(hub.Value);
        }
        return hubs;
    }

    public async Task<IEnumerable<dynamic>> GetProjects(string hubId, Tokens tokens)
    {
        var projects = new List<dynamic>();
        var api = new ProjectsApi();
        api.Configuration.AccessToken = tokens.InternalToken;
        var response = await api.GetHubProjectsAsync(hubId);
        foreach (KeyValuePair<string, dynamic> project in new DynamicDictionaryItems(response.data))
        {
            projects.Add(project.Value);
        }
        return projects;
    }

    public async Task<IEnumerable<dynamic>> GetContents(string hubId, string projectId, string folderId, Tokens tokens)
    {
        var contents = new List<dynamic>();
        if (string.IsNullOrEmpty(folderId))
        {
            var api = new ProjectsApi();
            api.Configuration.AccessToken = tokens.InternalToken;
            var response = await api.GetProjectTopFoldersAsync(hubId, projectId);
            foreach (KeyValuePair<string, dynamic> folders in new DynamicDictionaryItems(response.data))
            {
                contents.Add(folders.Value);
            }
        }
        else
        {
            var api = new FoldersApi();
            api.Configuration.AccessToken = tokens.InternalToken;
            var response = await api.GetFolderContentsAsync(projectId, folderId); // TODO: add paging
            foreach (KeyValuePair<string, dynamic> item in new DynamicDictionaryItems(response.data))
            {
                contents.Add(item.Value);
            }
        }
        return contents;
    }

    public async Task<IEnumerable<dynamic>> GetVersions(string hubId, string projectId, string itemId, Tokens tokens)
    {
        var versions = new List<dynamic>();
        var api = new ItemsApi();
        api.Configuration.AccessToken = tokens.InternalToken;
        var response = await api.GetItemVersionsAsync(projectId, itemId);
        foreach (KeyValuePair<string, dynamic> version in new DynamicDictionaryItems(response.data))
        {
            versions.Add(version.Value);
        }
        return versions;
    }

    public async Task<IEnumerable<dynamic>> GetSheetsContents(string projectId, string versionSet_Id, Tokens tokens)
    {
        var res_sheets_contents = new List<dynamic>();
        var projectId_without_b = projectId.Substring(2, projectId.Length - 2);

        if (string.IsNullOrEmpty(versionSet_Id))
        {

            //get all version sets
            string sheetVersionSetsUrl = string.Format("/construction/sheets/v1/projects/{0}/version-sets", projectId_without_b);
            RestClient client = new RestClient(APS_BASE_URL);
            RestRequest request = new RestRequest(sheetVersionSetsUrl, Method.Get);
            request.AddHeader("Authorization", string.Format("Bearer {0}", tokens.InternalToken));
            RestResponse response = await client.ExecuteAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var version_sets =
                        JsonConvert.DeserializeObject<Sheet_Version_Sets>(response.Content);
                foreach (Sheet_Version_Set set in version_sets.results)
                {
                    set.type = "sheetSet";
                    set.name = "<Sheet Set> " + set.name; // in order to differentiate with project folder
                    res_sheets_contents.Add(set);
                }
            }
        }
        else
        {
            //get sheets at specific version set
            string query = string.Format("?filter[versionSetId]={0}", versionSet_Id);
            string sheetVersionSetsUrl = string.Format("/construction/sheets/v1/projects/{0}/sheets" + query, projectId_without_b);
            RestClient client = new RestClient(APS_BASE_URL);
            RestRequest request = new RestRequest(sheetVersionSetsUrl, Method.Get);
            request.AddHeader("Authorization", string.Format("Bearer {0}", tokens.InternalToken));
            RestResponse response = await client.ExecuteAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var sheets =
                        JsonConvert.DeserializeObject<Sheets>(response.Content);
                foreach (Sheet sheet in sheets.results)
                {                    
                    
                    sheet.type = "sheet"; 
                    sheet.name = "<Number>" + sheet.number + " <Title>" + sheet.title;
                    res_sheets_contents.Add(sheet);
                }
            }

        }

        return res_sheets_contents;
    }
}
