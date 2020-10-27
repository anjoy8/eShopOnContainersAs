using Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Events;

namespace Basket.API.IntegrationEvents.Events
{
    /// <summary>
    /// 订单启动事件
    /// </summary>
    public class OrderStartedIntegrationEvent : IntegrationEvent
    {
        public string UserId { get; set; }

        public OrderStartedIntegrationEvent(string userId)
            => UserId = userId;            
    }
}
