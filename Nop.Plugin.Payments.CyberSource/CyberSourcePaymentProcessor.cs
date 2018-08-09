using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Plugins;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Web.Framework;

namespace Nop.Plugin.Payments.CyberSource
{
    /// <summary>
    /// CyberSource payment processor
    /// </summary>
    public class CyberSourcePaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly CurrencySettings _currencySettings;
        private readonly CyberSourcePaymentSettings _cyberSourcePaymentSettings;
        private readonly ICurrencyService _currencyService;
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        

        #endregion

        #region Ctor

        public CyberSourcePaymentProcessor(CurrencySettings currencySettings,
            CyberSourcePaymentSettings cyberSourcePaymentSettings,
            ICurrencyService currencyService,
            ILocalizationService localizationService,
            ISettingService settingService,
            IWebHelper webHelper)
        {
            this._currencySettings = currencySettings;
            this._cyberSourcePaymentSettings = cyberSourcePaymentSettings;
            this._currencyService = currencyService;
            this._localizationService = localizationService;
            this._settingService = settingService;
            this._webHelper = webHelper;
        }

        #endregion
        
        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult { NewPaymentStatus = PaymentStatus.Pending };
            return result;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var post = new RemotePost
            {
                FormName = "CyberSource",
                Url = _cyberSourcePaymentSettings.GatewayUrl,
                Method = "POST"
            };
            
            post.Add("merchantID", _cyberSourcePaymentSettings.MerchantId);
            post.Add("orderPage_timestamp", HostedPaymentHelper.OrderPageTimestamp);
            post.Add("orderPage_transactionType", "authorization");
            post.Add("orderPage_version", "4");
            post.Add("orderPage_serialNumber", _cyberSourcePaymentSettings.SerialNumber);

            post.Add("amount", string.Format(CultureInfo.InvariantCulture, "{0:0.00}", postProcessPaymentRequest.Order.OrderTotal));
            post.Add("currency", _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode);
            post.Add("orderNumber", postProcessPaymentRequest.Order.Id.ToString());

            post.Add("billTo_firstName", postProcessPaymentRequest.Order.BillingAddress.FirstName);
            post.Add("billTo_lastName", postProcessPaymentRequest.Order.BillingAddress.LastName);
            post.Add("billTo_street1", postProcessPaymentRequest.Order.BillingAddress.Address1);
            var billCountry = postProcessPaymentRequest.Order.BillingAddress.Country;
            if (billCountry != null)
            {
                post.Add("billTo_country", billCountry.TwoLetterIsoCode);
            }
            var billState = postProcessPaymentRequest.Order.BillingAddress.StateProvince;
            if (billState != null)
            {
                post.Add("billTo_state", billState.Abbreviation);
            }
            post.Add("billTo_city", postProcessPaymentRequest.Order.BillingAddress.City);
            post.Add("billTo_postalCode", postProcessPaymentRequest.Order.BillingAddress.ZipPostalCode);
            post.Add("billTo_phoneNumber", postProcessPaymentRequest.Order.BillingAddress.PhoneNumber);
            post.Add("billTo_email", postProcessPaymentRequest.Order.BillingAddress.Email);

            if (postProcessPaymentRequest.Order.ShippingStatus != ShippingStatus.ShippingNotRequired)
            {
                post.Add("shipTo_firstName", postProcessPaymentRequest.Order.ShippingAddress.FirstName);
                post.Add("shipTo_lastName", postProcessPaymentRequest.Order.ShippingAddress.LastName);
                post.Add("shipTo_street1", postProcessPaymentRequest.Order.ShippingAddress.Address1);
                var shipCountry = postProcessPaymentRequest.Order.ShippingAddress.Country;
                if (shipCountry != null)
                {
                    post.Add("shipTo_country", shipCountry.TwoLetterIsoCode);
                }
                var shipState = postProcessPaymentRequest.Order.ShippingAddress.StateProvince;
                if (shipState != null)
                {
                    post.Add("shipTo_state", shipState.Abbreviation);
                }
                post.Add("shipTo_city", postProcessPaymentRequest.Order.ShippingAddress.City);
                post.Add("shipTo_postalCode", postProcessPaymentRequest.Order.ShippingAddress.ZipPostalCode);
            }

            post.Add("orderPage_receiptResponseURL", $"{_webHelper.GetStoreLocation(false)}checkout/completed");
            post.Add("orderPage_receiptLinkText", "Return");

            post.Add("orderPage_signaturePublic", HostedPaymentHelper.CalcRequestSign(post.Params, _cyberSourcePaymentSettings.PublicKey));

            post.Post();
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return _cyberSourcePaymentSettings.AdditionalFee;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //CyberSource is the redirection payment method
            //it also validates whether order is also paid (after redirection) so customers will not be able to pay twice
            
            //payment status should be Pending
            if (order.PaymentStatus != PaymentStatus.Pending)
                return false;

            //let's ensure that at least 1 minute passed after order is placed
            return !((DateTime.UtcNow - order.CreatedOnUtc).TotalMinutes < 1);
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentCyberSource/Configure";
        }

        public string GetPublicViewComponentName()
        {
            return "PaymentCyberSource";
        }

        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }
        
        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            return paymentInfo;
        }

        public override void Install()
        {
            var settings = new CyberSourcePaymentSettings
            {
                GatewayUrl = "https://orderpagetest.ic3.com/hop/orderform.jsp",
                MerchantId = string.Empty,
                PublicKey = string.Empty,
                SerialNumber = string.Empty,
                AdditionalFee = 0,
            };
            _settingService.SaveSetting(settings);

            //locales
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.CyberSource.RedirectionTip", "You will be redirected to CyberSource site to complete the order.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.CyberSource.GatewayUrl", "Gateway URL");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.CyberSource.GatewayUrl.Hint", "Enter gateway URL.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.CyberSource.MerchantId", "Merchant ID");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.CyberSource.MerchantId.Hint", "Enter merchant ID.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.CyberSource.PublicKey", "Public Key");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.CyberSource.PublicKey.Hint", "Enter public key.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.CyberSource.SerialNumber", "Serial Number");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.CyberSource.SerialNumber.Hint", "Enter serial number.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.CyberSource.AdditionalFee", "Additional fee");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.CyberSource.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.CyberSource.PaymentMethodDescription", "You will be redirected to CyberSource site to complete the order.");
            
            base.Install();
        }

        public override void Uninstall()
        {
            _settingService.DeleteSetting<CyberSourcePaymentSettings>();

            //locales
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.CyberSource.RedirectionTip");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.CyberSource.GatewayUrl");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.CyberSource.GatewayUrl.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.CyberSource.MerchantId");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.CyberSource.MerchantId.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.CyberSource.PublicKey");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.CyberSource.PublicKey.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.CyberSource.SerialNumber");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.CyberSource.SerialNumber.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.CyberSource.AdditionalFee");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.CyberSource.AdditionalFee.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.CyberSource.PaymentMethodDescription");

            base.Uninstall();
        }

        #endregion

        #region Properies

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get
            {
                return RecurringPaymentType.NotSupported;
            }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.Redirection;
            }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription
        {
            get { return _localizationService.GetResource("Plugins.Payments.CyberSource.PaymentMethodDescription"); }
        }

        #endregion
    }
}
