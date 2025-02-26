using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;



namespace VoucherProject.Models
{
    public class VoucherList
    {
        public int VoucherId { get; set; }
        public string VoucherName { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; }
    }
}
