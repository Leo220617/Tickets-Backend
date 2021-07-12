using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace InversionGloblalWeb.Models
{
    public class RolesViewModel
    {
        
        public int idRol { get; set; }

        [StringLength(50)]
        public string NombreRol { get; set; }
    }
}
