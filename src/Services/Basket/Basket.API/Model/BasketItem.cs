using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.eShopOnContainers.Services.Basket.API.Model
{
    /// <summary>
    /// 购物车详情项
    /// </summary>
    public class BasketItem : IValidatableObject
    {
        public string Id { get; set; }
        /// <summary>
        /// 产品ID
        /// </summary>
        public int ProductId { get; set; }
        /// <summary>
        /// 产品名称
        /// 合理冗余（不能被修改）
        /// </summary>
        public string ProductName { get; set; }
        /// <summary>
        /// 单价
        /// </summary>
        public decimal UnitPrice { get; set; }
        /// <summary>
        /// 原单价
        /// 用于更新价格
        /// </summary>
        public decimal OldUnitPrice { get; set; }
        /// <summary>
        /// 数量
        /// </summary>
        public int Quantity { get; set; }
        /// <summary>
        /// 图片Url
        /// 合理冗余
        /// </summary>
        public string PictureUrl { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (Quantity < 1)
            {
                results.Add(new ValidationResult("Invalid number of units", new []{ "Quantity" }));
            }

            return results;
        }
    }
}
