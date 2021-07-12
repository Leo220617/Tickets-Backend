using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InversionGloblalWeb.Models
{
    public class LoginCambioClave
    {
        public string Email { get; set; }
        public string PasswordAnterior { get; set; }
        public string PasswordNuevo { get; set; }
    }
}
