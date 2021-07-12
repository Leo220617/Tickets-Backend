using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InversionGloblalWeb.Models
{
    public class Errores
    {
        public string ClassName { get; set; }
        public string Message { get; set; }
        public string Data { get; set; }
        public string InnerException { get; set; }
        public string HelpUrl { get; set; }
        public string StackTraceString { get; set; }
        public string RemoteStackString { get; set; }
        public int RemoteStackIndex { get; set; }
        public string ExceptionMethod { get; set; }
        public int HResult { get; set; }
        public string Source { get; set; }
        public string WatsonBuckets { get; set; }
    }
}
