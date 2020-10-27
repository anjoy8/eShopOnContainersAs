using Microsoft.eShopOnContainers.Services.Ordering.Domain.Seedwork;
using Ordering.Domain.Events;
using Ordering.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.eShopOnContainers.Services.Ordering.Domain.AggregatesModel.OrderAggregate
{
    public class Order
        : Entity, IAggregateRoot
    {
        // DDD 模式 建议
        // 使用私有字段(自EF Core 1.1以来就允许使用)是一种更好的封装
        // 与DDD聚合和域实体 保持一致,(而不是只会用属性或者属性集合)
        private DateTime _orderDate;

        // Address是一个值对象模式示例，持久化为EF Core 2.0拥有的实体
        public Address Address { get; private set; }

        public int? GetBuyerId => _buyerId;
        private int? _buyerId;

        public OrderStatus OrderStatus { get; private set; }
        private int _orderStatusId;

        private string _description;



        // 模拟的订单将此设置为true。目前我们不检查任何地方的草稿似的状态的订单，但如果需要，我们可以这样做
        private bool _isDraft;

        // DDD 模式 建议
        // 使用私有集合字段，更适合DDD聚合的封装
        // 因此，OrderItems不能从“AggregateRoot外部”直接添加到集合中，
        // 只能通过包含领域行为的方法OrderAggrergateRoot.AddOrderItem()来添加进来。
        private readonly List<OrderItem> _orderItems;
        public IReadOnlyCollection<OrderItem> OrderItems => _orderItems;

        private int? _paymentMethodId;

        public static Order NewDraft()
        {
            var order = new Order();
            order._isDraft = true;
            return order;
        }

        protected Order()
        {
            _orderItems = new List<OrderItem>();
            _isDraft = false;
        }

        // 添加订单方法，因为属性是私有的，只能通过构造函数添加
        public Order(string userId, string userName, Address address, int cardTypeId, string cardNumber, string cardSecurityNumber,
                string cardHolderName, DateTime cardExpiration, int? buyerId = null, int? paymentMethodId = null) : this()
        {
            _buyerId = buyerId;
            _paymentMethodId = paymentMethodId;
            _orderStatusId = OrderStatus.Submitted.Id;
            _orderDate = DateTime.UtcNow;
            Address = address;

            // 将OrderStarterDomainEvent添加到域事件集合 
            // 在更新到数据库{执行DbContext.SaveChanges()} 以后，触发/调度
            AddOrderStartedDomainEvent(userId, userName, cardTypeId, cardNumber,
                                       cardSecurityNumber, cardHolderName, cardExpiration);
        }

        // DDD 模式 建议
        // 这个Order AggregateRoot的方法 "AddOrderitem()" 应该是向订单添加项的唯一方法，
        // 因此，任何行为(折扣等)和验证都由聚合根AggregateRoot控制
        // 以保持整个聚合之间的一致性。
        public void AddOrderItem(int productId, string productName, decimal unitPrice, decimal discount, string pictureUrl, int units = 1)
        {
            // 是否存在某一个产品的订单
            var existingOrderForProduct = _orderItems.Where(o => o.ProductId == productId)
                .SingleOrDefault();

            if (existingOrderForProduct != null)
            {
                //如果discount大于当前的折扣，用更高的折扣和单位修改它。

                if (discount > existingOrderForProduct.GetCurrentDiscount())
                {
                    existingOrderForProduct.SetNewDiscount(discount);
                }

                existingOrderForProduct.AddUnits(units);
            }
            else
            {
                //添加经过验证的新订单项

                var orderItem = new OrderItem(productId, productName, unitPrice, discount, pictureUrl, units);
                _orderItems.Add(orderItem);
            }
        }

        // 设置支付Id
        public void SetPaymentId(int id)
        {
            _paymentMethodId = id;
        }

        // 设置买家Id
        public void SetBuyerId(int id)
        {
            _buyerId = id;
        }

        // 设置订单状态为：等待验证，MediatR总线
        public void SetAwaitingValidationStatus()
        {
            if (_orderStatusId == OrderStatus.Submitted.Id)
            {
                // 添加领域事件
                AddDomainEvent(new OrderStatusChangedToAwaitingValidationDomainEvent(Id, _orderItems));
                _orderStatusId = OrderStatus.AwaitingValidation.Id;
            }
        }

        // 设置订单状态为：库存确认
        public void SetStockConfirmedStatus()
        {
            if (_orderStatusId == OrderStatus.AwaitingValidation.Id)
            {
                AddDomainEvent(new OrderStatusChangedToStockConfirmedDomainEvent(Id));

                _orderStatusId = OrderStatus.StockConfirmed.Id;
                _description = "All the items were confirmed with available stock.";
            }
        }

        // 设置订单状态为：已支付
        public void SetPaidStatus()
        {
            if (_orderStatusId == OrderStatus.StockConfirmed.Id)
            {
                AddDomainEvent(new OrderStatusChangedToPaidDomainEvent(Id, OrderItems));

                _orderStatusId = OrderStatus.Paid.Id;
                _description = "The payment was performed at a simulated \"American Bank checking bank account ending on XX35071\"";
            }
        }

        // 设置订单状态为：已发货
        public void SetShippedStatus()
        {
            if (_orderStatusId != OrderStatus.Paid.Id)
            {
                StatusChangeException(OrderStatus.Shipped);
            }

            _orderStatusId = OrderStatus.Shipped.Id;
            _description = "The order was shipped.";
            AddDomainEvent(new OrderShippedDomainEvent(this));
        }

        // 设置订单状态为：已取消
        public void SetCancelledStatus()
        {
            if (_orderStatusId == OrderStatus.Paid.Id ||
                _orderStatusId == OrderStatus.Shipped.Id)
            {
                StatusChangeException(OrderStatus.Cancelled);
            }

            _orderStatusId = OrderStatus.Cancelled.Id;
            _description = $"The order was cancelled.";
            AddDomainEvent(new OrderCancelledDomainEvent(this));
        }

        // 设置订单状态为：当库存被拒绝时设置取消
        public void SetCancelledStatusWhenStockIsRejected(IEnumerable<int> orderStockRejectedItems)
        {
            if (_orderStatusId == OrderStatus.AwaitingValidation.Id)
            {
                _orderStatusId = OrderStatus.Cancelled.Id;

                var itemsStockRejectedProductNames = OrderItems
                    .Where(c => orderStockRejectedItems.Contains(c.ProductId))
                    .Select(c => c.GetOrderItemProductName());

                var itemsStockRejectedDescription = string.Join(", ", itemsStockRejectedProductNames);
                _description = $"The product items don't have stock: ({itemsStockRejectedDescription}).";
            }
        }

        // 添加订单开始领域命令，SaveChange()后触发
        private void AddOrderStartedDomainEvent(string userId, string userName, int cardTypeId, string cardNumber,
                string cardSecurityNumber, string cardHolderName, DateTime cardExpiration)
        {
            var orderStartedDomainEvent = new OrderStartedDomainEvent(this, userId, userName, cardTypeId,
                                                                      cardNumber, cardSecurityNumber,
                                                                      cardHolderName, cardExpiration);

            this.AddDomainEvent(orderStartedDomainEvent);
        }

        // 状态修改异常
        private void StatusChangeException(OrderStatus orderStatusToChange)
        {
            throw new OrderingDomainException($"Is not possible to change the order status from {OrderStatus.Name} to {orderStatusToChange.Name}.");
        }

        // 获取订单添加项的总数
        public decimal GetTotal()
        {
            return _orderItems.Sum(o => o.GetUnits() * o.GetUnitPrice());
        }
    }
}

