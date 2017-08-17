using System.Linq;
using System.Web.Mvc;
using Geeks.ProfilerAPI.Managers;
using Geeks.ProfilerAPI.Models;

namespace Geeks.ProfilerAPI.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var removedItems = BlackListManager.GetAll(Server);
            var reports = ReportManager.Get().Where(item => item.Count != 0).Where(item => !removedItems.Contains(item.Key))
                            .OrderByDescending(item => item.ElapsedTicks).ToArray();

            return View(reports);
        }

        public ActionResult Command(string key)
        {
            CommandManager.Set(new Command(key));

            return RedirectToAction("Index");
        }

        public ActionResult Remove(string key)
        {
            BlackListManager.Add(Server, key);

            return RedirectToAction("Index");
        }

        public ActionResult Reset()
        {
            BlackListManager.Reset(Server);

            return RedirectToAction("Index");
        }
    }
}
