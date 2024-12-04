using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace ShoppingCartV2.Models
{
    [MetadataType(typeof(ProductMetadata))]
    public partial class Product
    {
    }

    public class ProductMetadata
    {
        public int ProductID { get; set; }

        [Required(ErrorMessage = "The Product Tab value is required")]
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        [StringLength(30, ErrorMessage = "The Product Tab value cannot be more than 30 characters")]
        [Display(Name = "Product Tab")]
        public string ProductTab { get; set; }

        [StringLength(50, ErrorMessage = "The Product Name cannot be more than 50 characters")]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; }

        [StringLength(30, ErrorMessage = "The Image File name cannot be more than 30 characters")]
        [Display(Name = "Image File")]
        public string ImageFile { get; set; }

        [Required(ErrorMessage = "The Unit Price is required")]
        [Display(Name = "Unit Price")]
        public decimal UnitPrice { get; set; }

        [Required(ErrorMessage = "The Maximum Amount is required")]
        [Display(Name = "Max Amount")]
        public int MaxAmount { get; set; }

        [Required(ErrorMessage = "The Default Amount is required")]
        [Display(Name = "Default Amount")]
        public int DefaultAmount { get; set; }
    }
}