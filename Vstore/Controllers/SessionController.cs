using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Vstore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SessionController : ControllerBase
    {

        [HttpGet]
        public IEnumerable<string> GetSessionInfo()
        {
            List<string> sessionInfo = new List<string>();
            if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString(SessionVariables.SessionKeyUserName)))
            {
                HttpContext.Session.SetString(SessionKeysEnum.SessionKeyUserName.ToString(), "Current User");
                HttpContext.Session.SetString(SessionKeysEnum.SessionKeyUserId.ToString(), Guid.NewGuid().ToString());

            }
            var username = HttpContext.Session.GetString(SessionVariables.SessionKeyUserName);
            var SessionId = HttpContext.Session.GetString(SessionVariables.SessionKeyUserId);

            sessionInfo.Add(username);
            sessionInfo.Add(SessionId);
            return sessionInfo;
        }
       
    }
}
