using Refit;
using Sicsoft.Checkin.Web.Models;
using System.Threading.Tasks;

namespace Sicsoft.Checkin.Web.Servicios
{
    public interface ICheckInClient
    {
        [Post("/Token")]
        Task<TokenResponseViewModel> GetToken([Body(BodySerializationMethod.UrlEncoded)] TokenRequestViewModel q);
    }
}
