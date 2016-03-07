using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.CyberSource.Models;
using Nop.Services.Configuration;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Framework.Controllers;

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

        public PaymentCyberSourceController(ISettingService settingService, 
            IPaymentService paymentService, IOrderService orderService, 
            IOrderProcessingService orderProcessingService, 
            CyberSourcePaymentSettings cyberSourcePaymentSettings,
            PaymentSettings paymentSettings)
        {
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._cyberSourcePaymentSettings = cyberSourcePaymentSettings;
            this._paymentSettings = paymentSettings;
        }
        
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            var model = new ConfigurationModel();
            model.GatewayUrl = _cyberSourcePaymentSettings.GatewayUrl;
            model.MerchantId = _cyberSourcePaymentSettings.MerchantId;
            model.PublicKey = _cyberSourcePaymentSettings.PublicKey;
            model.SerialNumber = _cyberSourcePaymentSettings.SerialNumber;
            model.AdditionalFee = _cyberSourcePaymentSettings.AdditionalFee;

            return View("~/Plugins/Payments.CyberSource/Views/PaymentCyberSource/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
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

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            var model = new PaymentInfoModel();
            return View("~/Plugins/Payments.CyberSource/Views/PaymentCyberSource/PaymentInfo.cshtml", model);
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            return paymentInfo;
        }

        [ValidateInput(false)]
        public ActionResult IPNHandler(FormCollection form)
        {
            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.CyberSource") as CyberSourcePaymentProcessor;
            if (processor == null ||
                !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("CyberSource module cannot be loaded");

            if (HostedPaymentHelper.ValidateResponseSign(form, _cyberSourcePaymentSettings.PublicKey))
            {
                string reasonCode = form["reasonCode"];
                if (!String.IsNullOrEmpty(reasonCode) && reasonCode.Equals("100"))
                {
                    int orderId;
                    if (Int32.TryParse(form["orderNumber"], out orderId))
                    {
                        var order = _orderService.GetOrderById(orderId);
                        if (order != null && _orderProcessingService.CanMarkOrderAsAuthorized(order))
                        {
                            _orderProcessingService.MarkAsAuthorized(order);
                        }
                    }
                }
            }

            return Content("");

        }
    }
}