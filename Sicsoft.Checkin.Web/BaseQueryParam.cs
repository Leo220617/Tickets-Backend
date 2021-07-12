using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sicsoft.Checkin.Web
{
    public class BaseQueryParam
    {
        public string Skip { get; set; }
        public string Take { get; set; }

        public string SearchText { get; set; }
    }
}
