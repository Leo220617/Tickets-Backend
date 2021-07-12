namespace Sicsoft.Checkin.Web.Models
{
    public class TokenResponseViewModel
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string userName { get; set; }
    }
}
