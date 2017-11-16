using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.CyberSource
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IRouteBuilder routeBuilder)
        {
            //IPN
            routeBuilder.MapRoute("Plugin.Payments.CyberSource.IPNHandler",
                 "Plugins/PaymentCyberSource/IPNHandler",
                 new { controller = "PaymentCyberSource", action = "IPNHandler" });
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
