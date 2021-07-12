using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sicsoft.Checkin.Web
{
    public static class IFormFileExtensions
    {
        public static string ConvertirBase64(this IFormFile file)
        {

            if (file == null)
                return null;

            var stream = file.OpenReadStream();

            using var ms = new MemoryStream();
            file.CopyTo(ms);
            var fileBytes = ms.ToArray();
            string s = Convert.ToBase64String(fileBytes);
            // act on the Base64 data
            return s;
        }
    }
}
