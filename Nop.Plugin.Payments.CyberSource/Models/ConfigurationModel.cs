using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Mvc.Models;

namespace Nop.Plugin.Payments.CyberSource.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Payments.CyberSource.GatewayUrl")]
        public string GatewayUrl { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CyberSource.MerchantId")]
        public string MerchantId { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CyberSource.PublicKey")]
        public string PublicKey { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CyberSource.SerialNumber")]
        public string SerialNumber { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CyberSource.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
    }
}