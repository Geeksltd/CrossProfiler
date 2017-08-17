using System.Web.Http;
using Geeks.ProfilerAPI.Managers;

namespace Geeks.ProfilerAPI.Controllers
{
    public class CommandApiController : ApiController
    {
        [Route("api/command")]
        public string Get()
        {
            return CommandManager.Get().ToString();
        }
    }
}
