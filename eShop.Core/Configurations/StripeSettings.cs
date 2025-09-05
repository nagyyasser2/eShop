namespace eShop.Core.Configurations
{
    public class StripeSettings
    {
        public string PublishableKey { get; set; }
        public long WebhookTolerance { get; set; }
        public string SecretKey { get; set; }
        public string WebhookSecret { get; set; }
        public string Currency { get; set; } = "usd";
        public string WebhookEndpoint { get; set; }
        public string SuccessUrl {get;set;}
        public string CancelUrl { get;set;}
    }
}
