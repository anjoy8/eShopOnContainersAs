namespace Microsoft.eShopOnContainers.Services.Identity.API
{
    /// <summary>
    /// 配合appsettings.json，获取配置数据
    /// </summary>
    public class AppSettings
    {
        public string MvcClient { get; set; }

        public bool UseCustomizationData { get; set; }
    }
}
