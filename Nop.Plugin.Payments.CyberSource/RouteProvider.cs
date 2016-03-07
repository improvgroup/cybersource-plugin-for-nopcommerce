using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.CyberSource
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            //IPN
            routes.MapRoute("Plugin.Payments.CyberSource.IPNHandler",
                 "Plugins/PaymentCyberSource/IPNHandler",
                 new { controller = "PaymentCyberSource", action = "IPNHandler" },
                 new[] { "Nop.Plugin.Payments.CyberSource.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
