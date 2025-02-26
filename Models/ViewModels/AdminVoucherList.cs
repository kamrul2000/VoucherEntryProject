using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VoucherProject.Models.ViewModels
{
    public class AdminVoucherList
    {
        public User User { get; set; }
        public VoucherViewModel VoucherViewModel { get; set; }
        
    }
}

// AdminVoucherList.User.Name = 'User Name'