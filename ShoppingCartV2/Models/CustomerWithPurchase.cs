using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShoppingCartV2.Models
{
    public class CustomerWithPurchase
    {
        public int ID { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public decimal TotalPrice { get; set; }

        public int OrderID { get; set; }
    }
}