using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Sicsoft.Checkin.Web
{
    public class AuthenticatedHttpClientHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public AuthenticatedHttpClientHandler(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
           
            
            if (!request.RequestUri.PathAndQuery.Contains("Token"))
            {
                var claim = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.UserData);
              
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", claim.Value);
                
            }
           
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            //if(response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            //{
            //    var responseRedirect = new HttpResponseMessage(HttpStatusCode.Redirect);
            //    response.Headers.Location = new System.Uri($"{request.RequestUri.Host}:{request.RequestUri}Accout/Login");
            //    var tsc = new TaskCompletionSource<HttpResponseMessage>();
            //    tsc.SetResult(responseRedirect);
              
            //    httpContextAccessor.HttpContext.Response.Redirect($"{request.RequestUri.Host}:{request.RequestUri}Accout/Login");
            //    return await tsc.Task;

            //}
            return response;
        }


    }
}
