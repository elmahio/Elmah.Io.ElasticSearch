using System.Web.Mvc;

namespace Elmah.Io.ElasticSearch.Web.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            return RedirectToAction("Index", "Elmah");
        }

    }
}
