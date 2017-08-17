using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Geeks.ProfilerAPI.Managers;

namespace Geeks.ProfilerAPI.Controllers
{
    public class ReportApiController : ApiController
    {
        [HttpPost]
        [Route("api/report")]
        public async Task<IHttpActionResult> Report()
        {
            var provider = new MultipartMemoryStreamProvider();
            await Request.Content.ReadAsMultipartAsync(provider);
            foreach (var file in provider.Contents)
            {
                var content = file.ReadAsStringAsync().Result;
                ReportManager.Save(content);
            }

            return Ok();
        }
    }
}
