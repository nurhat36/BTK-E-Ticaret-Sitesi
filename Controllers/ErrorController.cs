using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BTKETicaretSitesi.Controllers
{
    [EnableRateLimiting("GenelSiteLimiti")]
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        [AllowAnonymous]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            switch (statusCode)
            {
                case 404:
                    return View("NotFound");
                case 403:
                    return View("AccessDenied");
                default:
                    return View("Error");
            }
        }

        [Route("Error")]
        [AllowAnonymous]
        public IActionResult Error()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            // Geliştirme ortamında daha fazla detay göster
            //if (Environment.IsDevelopment())
            //{
            //    ViewBag.ErrorMessage = exceptionHandlerPathFeature?.Error.Message;
            //    ViewBag.StackTrace = exceptionHandlerPathFeature?.Error.StackTrace;
            //    ViewBag.Path = exceptionHandlerPathFeature?.Path;
            //}

            return View("Error");
        }
    }
}
