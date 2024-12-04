using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace ShoppingCartV2.Models
{
    [MetadataType(typeof(OptionMetadata))]
    public partial class Option
    {
    }

    public class OptionMetadata
    {
        public int OptionID { get; set; }

        [Required(ErrorMessage = "The Option Type field is required")]
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        [StringLength(30, ErrorMessage = "The Option Type cannot be more than 30 characters")]
        public string OptionType { get; set; }

        [Required(ErrorMessage = "The Option Name field is required")]
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        [StringLength(50, ErrorMessage = "The Option Name cannot be more than 50 characters")]
        public string OptionName { get; set; }

        [Required(ErrorMessage = "The Option Cost is required")]
        public decimal OptionCost { get; set; }
    }
}