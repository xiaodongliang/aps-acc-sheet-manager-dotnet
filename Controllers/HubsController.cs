using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
 
[ApiController]
[Route("api/[controller]")]
public class HubsController : ControllerBase
{
    private readonly ILogger<HubsController> _logger;
    private readonly APS _aps;

    public HubsController(ILogger<HubsController> logger, APS aps)
    {
        _logger = logger;
        _aps = aps;
    }

    [HttpGet()]
    public async Task<ActionResult<string>> ListHubs()
    {
        var tokens = await AuthController.PrepareTokens(Request, Response, _aps);
        if (tokens == null)
        {
            return Unauthorized();
        }
        var hubs = await _aps.GetHubs(tokens);
        return JsonConvert.SerializeObject(hubs);
    }

    [HttpGet("{hub}/projects")]
    public async Task<ActionResult<string>> ListProjects(string hub)
    {
        var tokens = await AuthController.PrepareTokens(Request, Response, _aps);
        if (tokens == null)
        {
            return Unauthorized();
        }
        var projects = await _aps.GetProjects(hub, tokens);
        return JsonConvert.SerializeObject(projects);
    }

    [HttpGet("{hub}/projects/{project}/contents")]
    public async Task<ActionResult<string>> ListItems(string hub, string project, [FromQuery] string? folder_id,[FromQuery] string? set_id)
    {
        var tokens = await AuthController.PrepareTokens(Request, Response, _aps);
        if (tokens == null)
        {
            return Unauthorized();
        }
        
       
        dynamic obj = new JObject();
        obj.folderContents = JsonConvert.SerializeObject(new List<dynamic>()); 
        obj.sheetsContents = JsonConvert.SerializeObject(new List<dynamic>()); 
        if(folder_id == null && set_id == null){
            //will extract folder and sheet version sets
            var folderContents = await _aps.GetContents(hub, project, folder_id, tokens);
            var sheetsContents = await _aps.GetSheetsContents(project, set_id,tokens);

            obj.folderContents =  JsonConvert.SerializeObject(folderContents); 
            obj.sheetsContents =  JsonConvert.SerializeObject(sheetsContents);
        }else if(folder_id != null && set_id == null){
            //will extract  folder contents
            var folderContents = await _aps.GetContents(hub, project, folder_id, tokens);
            obj.folderContents =  JsonConvert.SerializeObject(folderContents); 

        }else if(folder_id == null && set_id != null){
             //will extract  sheets in one version set 
            var sheetsContents = await _aps.GetSheetsContents( project, set_id, tokens);
            obj.sheetsContents =  JsonConvert.SerializeObject(sheetsContents); 
        }
        
        return JsonConvert.SerializeObject(obj);
    }

    [HttpGet("{hub}/projects/{project}/contents/{item}/versions")]
    public async Task<ActionResult<string>> ListVersions(string hub, string project, string item)
    {
        var tokens = await AuthController.PrepareTokens(Request, Response, _aps);
        if (tokens == null)
        {
            return Unauthorized();
        }
        var versions = await _aps.GetVersions(hub, project, item, tokens);
        return JsonConvert.SerializeObject(versions);
    }
}
