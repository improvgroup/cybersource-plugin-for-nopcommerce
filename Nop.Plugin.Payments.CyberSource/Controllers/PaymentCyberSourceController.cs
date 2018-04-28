using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.CyberSource.Models;
using Nop.Services.Configuration;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.CyberSource.Controllers
{
    public class PaymentCyberSourceController : BasePaymentController
    {
        private readonly ISettingService _settingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly CyberSourcePaymentSettings _cyberSourcePaymentSettings;
        private readonly PaymentSettings _paymentSettings;
        private readonly IPermissionService _permissionService;

        public PaymentCyberSourceController(ISettingService settingService, 
            IPaymentService paymentService, IOrderService orderService, 
            IOrderProcessingService orderProcessingService, 
            CyberSourcePaymentSettings cyberSourcePaymentSettings,
            PaymentSettings paymentSettings,
            IPermissionService permissionService)
        {
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._cyberSourcePaymentSettings = cyberSourcePaymentSettings;
            this._paymentSettings = paymentSettings;
            this._permissionService = permissionService;
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            var model = new ConfigurationModel
            {
                GatewayUrl = _cyberSourcePaymentSettings.GatewayUrl,
                MerchantId = _cyberSourcePaymentSettings.MerchantId,
                PublicKey = _cyberSourcePaymentSettings.PublicKey,
                SerialNumber = _cyberSourcePaymentSettings.SerialNumber,
                AdditionalFee = _cyberSourcePaymentSettings.AdditionalFee
            };

            return View("~/Plugins/Payments.CyberSource/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            //save settings
            _cyberSourcePaymentSettings.GatewayUrl = model.GatewayUrl;
            _cyberSourcePaymentSettings.MerchantId = model.MerchantId;
            _cyberSourcePaymentSettings.PublicKey = model.PublicKey;
            _cyberSourcePaymentSettings.SerialNumber = model.SerialNumber;
            _cyberSourcePaymentSettings.AdditionalFee = model.AdditionalFee;
            _settingService.SaveSetting(_cyberSourcePaymentSettings);

            return Configure();
        }
        
        public IActionResult IPNHandler(IpnModel model)
        {
            var form = model.Form;
            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.CyberSource") as CyberSourcePaymentProcessor;
            if (processor == null ||
                !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("CyberSource module cannot be loaded");

            var reasonCode = form["reasonCode"];

            if (HostedPaymentHelper.ValidateResponseSign(form, _cyberSourcePaymentSettings.PublicKey) &&
                !string.IsNullOrEmpty(reasonCode) && reasonCode.Equals("100") &&
                int.TryParse(form["orderNumber"], out int orderId))
            {
                var order = _orderService.GetOrderById(orderId);
                if (order != null && _orderProcessingService.CanMarkOrderAsAuthorized(order))
                {
                    _orderProcessingService.MarkAsAuthorized(order);
                }
            }

            return Content("");
        }
    }
}