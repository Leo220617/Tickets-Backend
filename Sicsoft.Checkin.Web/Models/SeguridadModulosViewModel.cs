using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace InversionGloblalWeb.Models
{
    public class SeguridadModulosViewModel
    {
         
        public int CodModulo { get; set; }

        [Required]
        [StringLength(150)]
        public string Descripcion { get; set; }
    }
}
