using System;

namespace VoucherProject.Models
{
    public class VoucherViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime SubmitDate { get; set; }
        public DateTime ParticularDate { get; set; }
        public string Particular { get; set; }
        public string Remarks { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Pending";

    }
}

