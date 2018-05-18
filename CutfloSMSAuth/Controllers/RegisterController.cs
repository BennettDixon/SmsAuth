using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CutfloSMSAuth.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using CutfloSMSAuth.Constants;
using System.Net.Http;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CutfloSMSAuth.Controllers
{
    public class RegisterController : Controller
    {
        private readonly string PhoneKey = HttpContextKeys.Phone;
        private readonly string EmailKey = HttpContextKeys.Email;
        private readonly string TokenKey = HttpContextKeys.Token;
        private string CurrentKey { get; set; }

        private readonly UserSqlContext sqlContext;
        //private readonly UserContext entityContext;


        public RegisterController(UserSqlContext _sqlContext)
        {
            sqlContext = _sqlContext;
        }

        // Registration Methods \\

        // REQUIRED ATTRIBUTES FOR THIS POST OBJECT : User.ApiKey, User.CompanyName, User.CompanyMailingUrl, User.Email OR User.PhoneNumber
        // OPTIONAL : User.FirstName
        [Route("api/registration/login/validate")]
        [HttpPost]
        public async Task<IActionResult> ValidatePhone([FromBody] User post)
        {
            var apiUser = await sqlContext.AuthApiUser(post.ApiKey);

            if (apiUser == null) return Json(ErrorMessages.ApiAuthenticationError());

            string email = post.Email;
            string phoneNumber = post.PhoneNumber;
            User sqlUser = null;
            var jsonFalse = Json(new { success = false });

            if ((phoneNumber == null && email == null))
            {
                return jsonFalse;
            }

            if (phoneNumber != null)
            {
                sqlUser = sqlContext.GetUserByPhone(phoneNumber);
            }
            if (email != null && phoneNumber == null)
            {
                sqlUser = sqlContext.GetUserByEmail(email);
            }
            if (sqlUser == null)
            {
                return jsonFalse;
            }
            /////////\\\\\\\\\\
            // SEND THE TOKEN \\\
            //\\\\\\\\\//////////\\
            // Send via phone
            if (phoneNumber != null)
            {
                string _token = sqlUser.SendSmsToken();
                HttpContext.Session.SetString(PhoneKey, phoneNumber);
                HttpContext.Session.SetString(TokenKey, _token);
                await UptickApiUserSmsCount(apiUser);
                return Json(GenericJsonSuccess.True);
            }
            // Send via email if no phone
            else if (phoneNumber == null && email != null)
            {
                bool isRegistration = false;
                string _token = sqlUser.SendEmailToken(isRegistration);
                HttpContext.Session.SetString(EmailKey, email);
                HttpContext.Session.SetString(TokenKey, _token);
                await UptickApiUserSmsCount(apiUser);
                return Json(GenericJsonSuccess.True);
            }

            return Json(GenericJsonSuccess.False);
        }

        // REQUIRED ATTRIBUTES FOR THIS POST OBJECT : User.ApiKey, User.Token
        [Route("api/registration/login/auth/")]
        [HttpPost]
        public async Task<IActionResult> Auth([FromBody] User post)
        {
            string token = post.Token;
            User user = GetSqlUser();

            if (user != null && token == HttpContext.Session.GetString(TokenKey))
            {
                HttpContext.Session.Remove(TokenKey);
                HttpContext.Session.Remove(PhoneKey);
                string _sessionId = await sqlContext.SetLoginSessionId(user);
                //var _userWithPremiumInfo = sqlContext.CheckPremium(user);
                //var returnUser = await sqlContext.GetAdditionalUserInfo(_userWithPremiumInfo);
                return Json(user);
            }
            return Json(null);
        }

        private User GetSqlUser()
        {
            if (HttpContext.Session.Keys.Contains(PhoneKey))
            {
                var phone = HttpContext.Session.GetString(PhoneKey);
                var user = sqlContext.GetUserByPhone(phone);
                CurrentKey = PhoneKey;
                return user;
            }
            else if (HttpContext.Session.Keys.Contains(EmailKey))
            {
                var email = HttpContext.Session.GetString(EmailKey);
                var user = sqlContext.GetUserByEmail(email);
                CurrentKey = EmailKey;
                return user;
            }
            else
            {
                return null;
            }
        }

        // REQUIRED ATTRIBUTES FOR THIS POST OBJECT :  User.ApiKey, User.Email OR User.PhoneNumber, User.CompanyName, User.CompanyMailingUrl  
        // NOTE YOU SHOULD CHECK TO SEE IF THE USER EXISTS IN YOUR DATABASE BEFORE SENDING THE POST REQUEST
        [Route("api/registration/register/validate")]
        [HttpPost]
        public async Task<IActionResult> ValidateSms([FromBody] User post)
        {
            User user = null;
            string email = post.Email;
            string phoneNumber = post.PhoneNumber;
            bool isEmail = false;

            if (phoneNumber == null && email == null)
            {
                return Json(ErrorMessages.ContactMethodError());
            }

            if (phoneNumber == null && email != null) isEmail = true;

            /////////\\\\\\\\\\
            // SEND THE TOKEN \\\
            //\\\\\\\\\//////////\\

            if (user == null)
            {
                if (isEmail)
                {
                    user = new User { Email = email, FirstName = null };
                }
                else
                {
                    user = new User() { PhoneNumber = phoneNumber, FirstName = null };
                }
            }

            // Send via phone
            if (phoneNumber != null)
            {
                string _token = user.SendSmsToken();
                user.Token = _token;
            }
            // Send via email if there is no phone
            else if (email != null && phoneNumber == null)
            {
                bool isRegistration = true;
                string _token = user.SendEmailToken(isRegistration);
                user.Token = _token;
            }

            var respBool = await sqlContext.CreateTempUser(user);

            if (respBool == false)
            {
                SqlDebugger.Instance.ServerWrite("Failed To Create temp user");
            }

            return Json(GenericJsonSuccess.True);

        }

        // REQUIRED ATTRIBUTES FOR THIS POST OBJECT : User.Token
        [Route("api/registration/register/auth/")]
        [HttpPost]
        public async Task<IActionResult> RegAuth([FromBody] User post)
        {
            string token = post.Token;
            User user = sqlContext.GetTempUserFromToken(token);

            if (user != null && token == user.Token)
            {
                await sqlContext.SetRegistrationSession(user);
                sqlContext.DeleteTempUser(user);
                return Json(user.RegistrationSession);
            }

            return Json(user.RegistrationSession);
        }

        private async Task<bool> UptickApiUserSmsCount(User apiUser)
        {
            bool upticked = await sqlContext.UptickUserSmsCount(apiUser);
            if (upticked == false) SqlDebugger.Instance.ServerWrite("Failed to log sms uptick, userid: " + apiUser.UserId);
            return upticked;
        }
    }
}