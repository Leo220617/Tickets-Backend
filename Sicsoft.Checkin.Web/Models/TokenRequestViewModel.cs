using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sicsoft.Checkin.Web.Models
{
    public class TokenRequestViewModel
    {
        public string grant_type { get; set; } = "password";
        public string userName { get; set; }
        public string password { get; set; }
    }
}
