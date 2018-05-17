using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CutfloSMSAuth.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CutfloSMSAuth.Controllers
{
    public class UserController : Controller
    {
        private readonly UserSqlContext sqlContext;

        public UserController(UserSqlContext _sqlContext)
        {
            sqlContext = _sqlContext;
        }


        [Route("/api/user/email/login/validate")]
        [HttpPost]
        public IActionResult ValidateEmail(string email)
        {
            if (email == null)
            {
                return Json(new { success = false });
            }

            var user = sqlContext.GetUserByEmail(email);

            //If there is someone to send a token to, send a token.
            if (user != null)
            {
                bool isRegistration = false;
                string _token = user.SendEmailToken(isRegistration);
                HttpContext.Session.SetString("Email", user.Email);
                HttpContext.Session.SetString("Token", _token);
                return Json(new { success = true });
            }

            return Json(new { success = false });

        }

        [Route("api/user/sms/login/validate")]
        [HttpPost]
        public IActionResult ValidatePhone(string phoneNumber)
        {
            if (phoneNumber == null)
            {
                return Json(new { success = false });
            }

            var user = sqlContext.GetUserByPhone(phoneNumber);

            //If there is someone to send a token to, send a token.
            if (user != null)
            {
                string _token = user.SendSmsToken();
                HttpContext.Session.SetString("PhoneNumber", user.PhoneNumber);
                HttpContext.Session.SetString("Token", _token);
                return Json(new { success = true });
            }

            return Json(new { success = false });
            
        }

        [Route("api/user/email/login/auth/")]
        [HttpPost]
        public IActionResult AuthEmail(string token)
        {
            var jsonFalse = Json(new { success = false });

            //If there is no session data, AKA a Token, return false
            if (!HttpContext.Session.Keys.Any()) return jsonFalse;

            string email = HttpContext.Session.GetString("Email");

            var user = sqlContext.GetUserByEmail(email);

            if (user != null && token == HttpContext.Session.GetString("Token"))
            {
                HttpContext.Session.Remove("Token");
                HttpContext.Session.Remove("Email");
                string _sessionId = sqlContext.SetLoginSessionId(user, out int rowsEff);
                return Json(new { success = true, LoginSession = _sessionId, RowsEffected = rowsEff });
            }

            return jsonFalse;
        }

        [Route("api/user/sms/login/auth/")]
        [HttpPost]
        public IActionResult AuthPhone(string token)
        {
            var jsonFalse = Json(new { success = false });

            //If there is no session data, AKA a Token, return false
            if (!HttpContext.Session.Keys.Any()) return jsonFalse;

            string number = HttpContext.Session.GetString("PhoneNumber");

            var user = sqlContext.GetUserByPhone(number);   

            if (user != null && token == HttpContext.Session.GetString("Token"))
            {
                HttpContext.Session.Remove("Token");
                HttpContext.Session.Remove("PhoneNumber");
                string _sessionId = sqlContext.SetLoginSessionId(user, out int rowsEff);
                return Json(new { success = true, LoginSession = _sessionId, RowsEffected = rowsEff });
            }

            return jsonFalse;
        }
    }
}
