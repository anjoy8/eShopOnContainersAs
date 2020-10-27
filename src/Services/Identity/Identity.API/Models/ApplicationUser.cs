using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.eShopOnContainers.Services.Identity.API.Models
{
    // 通过向ApplicationUser类添加属性来为应用程序用户添加配置文件数据
    /// <summary>
    /// 自定义用户数据
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string CardNumber { get; set; }
        [Required]
        public string SecurityNumber { get; set; }
        [Required]
        [RegularExpression(@"(0[1-9]|1[0-2])\/[0-9]{2}", ErrorMessage = "Expiration should match a valid MM/YY value")]
        public string Expiration { get; set; }
        [Required]
        public string CardHolderName { get; set; }
        public int CardType { get; set; }
        [Required]
        public string Street { get; set; }
        [Required]
        public string City { get; set; }
        [Required]
        public string State { get; set; }
        [Required]
        public string Country { get; set; }
        [Required]
        public string ZipCode { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string LastName { get; set; }
    }
}
