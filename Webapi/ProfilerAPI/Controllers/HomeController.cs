using System.Linq;
using System.Web.Mvc;
using Geeks.ProfilerAPI.Managers;
using Geeks.ProfilerAPI.Models;
using Geeks.ProfilerAPI.ViewModels.Home;

namespace Geeks.ProfilerAPI.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var model = new IndexViewModel();

            model.Command = CommandManager.Get().ToString();

            var removedItems = BlackListManager.GetAll(Server);
            model.Reports = ReportManager.Get().Where(item => item.Count != 0).Where(item => !removedItems.Contains(item.Key))
                .OrderByDescending(item => item.ElapsedTicks).ToArray();

            return View(model);
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

        public ActionResult Clear()
        {
            ReportManager.Clear();

            return RedirectToAction("Index");
        }
    }
}
