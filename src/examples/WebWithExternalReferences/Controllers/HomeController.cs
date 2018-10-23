using System;
using System.Web.Mvc;
using Elmah;

namespace WebWithExternalReferences.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            WriteElmahException(new Exception("this exception should be logged with every page load"));
            return View();
        }

        private void WriteElmahException(Exception ex)
        {
            var elmahCon = ErrorSignal.FromCurrentContext();
            elmahCon.Raise(ex);
        }
    }
}