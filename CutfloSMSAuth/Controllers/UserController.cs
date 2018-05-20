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
    public class UserController : Controller
    {
        private readonly string PhoneKey = HttpContextKeys.Phone;
        private readonly string EmailKey = HttpContextKeys.Email;
        private readonly string TokenKey = HttpContextKeys.Token;
        private string CurrentKey { get; set; }

        private readonly UserSqlContext sqlContext;
        //private readonly UserContext entityContext;


        public UserController(UserSqlContext _sqlContext)
        {
            sqlContext = _sqlContext;
        }

        // Registration Methods \\

        // REQUIRED ATTRIBUTES FOR THIS POST OBJECT : User.ApiKey, User.Email OR User.PhoneNumber
        // OPTIONAL : User.FirstName
        // NOTE VALIDATE USER EXISTS IN DATABASE BY THEIR CONTACT METHOD BEFORE USING THIS ENDPOINT
        [Route("api/user/login/validate")]
        [HttpPost]
        public async Task<IActionResult> ValidatePhone([FromBody] User post)
        {
            var apiUser = await sqlContext.AuthApiUser(post.ApiKey);

            if (apiUser == null) return Json(ErrorMessages.ApiAuthenticationError());

            string email = post.Email;
            string phoneNumber = post.PhoneNumber;
            User userObject = null;
            var jsonFalse = Json(new { success = false });

            if ((phoneNumber == null && email == null))
            {
                return jsonFalse;
            }

            userObject.CompanyName = apiUser.CompanyName;
            userObject.CompanyMailingUrl = apiUser.CompanyMailingUrl;
            
            /////////\\\\\\\\\\
            // SEND THE TOKEN \\\
            //\\\\\\\\\//////////\\
            // Send via phone
            if (phoneNumber != null)
            {
                userObject.PhoneNumber = phoneNumber;
                string _token = userObject.SendSmsToken();
                HttpContext.Session.SetString(PhoneKey, phoneNumber);
                HttpContext.Session.SetString(TokenKey, _token);
                await UptickApiUserSmsCount(apiUser);
                return Json(GenericJsonSuccess.True);
            }
            // Send via email if no phone
            else if (phoneNumber == null && email != null)
            {
                bool isRegistration = false;
                userObject.Email = email;
                string _token = userObject.SendEmailToken(isRegistration);
                HttpContext.Session.SetString(EmailKey, email);
                HttpContext.Session.SetString(TokenKey, _token);
                await UptickApiUserSmsCount(apiUser);
                return Json(GenericJsonSuccess.True);
            }

            return Json(GenericJsonSuccess.False);
        }

        // REQUIRED ATTRIBUTES FOR THIS POST OBJECT : User.ApiKey, User.Token
        [Route("api/user/login/auth/")]
        [HttpPost]
        public async Task<IActionResult> Auth([FromBody] User post)
        {
            var apiUser = await sqlContext.AuthApiUser(post.ApiKey);

            if (apiUser == null) return Json(ErrorMessages.ApiAuthenticationError());

            string token = post.Token;

            if (token == HttpContext.Session.GetString(TokenKey))
            {
                HttpContext.Session.Remove(TokenKey);
                HttpContext.Session.Remove(PhoneKey);
                string _sessionId = KeyGeneration.GenerateSession();
                //var _userWithPremiumInfo = sqlContext.CheckPremium(user);
                //var returnUser = await sqlContext.GetAdditionalUserInfo(_userWithPremiumInfo);
                return Json(new SessionResponse(_sessionId));
            }
            return Json(null);
        }

        // REQUIRED ATTRIBUTES FOR THIS POST OBJECT :  User.ApiKey, User.Email OR User.PhoneNumber
        // NOTE YOU SHOULD CHECK TO SEE IF THE USER EXISTS IN YOUR DATABASE BY THEIR CONTACT METHOD BEFORE USING THIS ENDPOINT
        [Route("api/user/register/validate")]
        [HttpPost]
        public async Task<IActionResult> ValidateSms([FromBody] User post)
        {
            var apiUser = await sqlContext.AuthApiUser(post.ApiKey);

            if (apiUser == null) return Json(ErrorMessages.ApiAuthenticationError());
            SqlDebugger.Instance.ServerWrite("APIUSER authenticated");
            //AUTHING SUCCESSFULLY ERROR IS DOWN THE LINE 

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
                    user = new User
                    {
                        Email = email,
                        FirstName = null
                    };
                }
                else
                {
                    user = new User() { PhoneNumber = phoneNumber, FirstName = null };
                }
                user.CompanyMailingUrl = apiUser.CompanyMailingUrl;
                user.CompanyName = apiUser.CompanyName;
            }

            // Send via phone
            if (phoneNumber != null)
            {
                SqlDebugger.Instance.ServerWrite("attempting to send token");
                string _token = user.SendSmsToken();
                SqlDebugger.Instance.ServerWrite("token sent: " + _token);
                user.Token = _token;
                await UptickApiUserSmsCount(apiUser);
            }
            // Send via email if there is no phone
            else if (email != null && phoneNumber == null)
            {
                bool isRegistration = true;
                string _token = user.SendEmailToken(isRegistration);
                user.Token = _token;
                await UptickApiUserSmsCount(apiUser);
            }

            var respBool = await sqlContext.CreateTempUser(user);

            if (respBool == false)
            {
                SqlDebugger.Instance.SetDebugContext(apiUser.UserId);
                SqlDebugger.Instance.ServerWrite("Failed To Create temp user");
            }

            return Json(GenericJsonSuccess.True);

        }

        // REQUIRED ATTRIBUTES FOR THIS POST OBJECT : User.Token
        [Route("api/user/register/auth/")]
        [HttpPost]
        public async Task<IActionResult> RegAuth([FromBody] User post)
        {
            var apiUser = await sqlContext.AuthApiUser(post.ApiKey);

            if (apiUser == null) return Json(ErrorMessages.ApiAuthenticationError());

            string token = post.Token;
            User user = sqlContext.GetTempUserFromToken(token);

            if (user != null && token == user.Token)
            {
                await sqlContext.SetRegistrationSession(user);
                sqlContext.DeleteTempUser(user);
                return Json(user.RegistrationSession);
            }

            return Json(new SessionResponse(user.RegistrationSession));
        }

        private async Task<bool> UptickApiUserSmsCount(User apiUser)
        {
            bool upticked = await sqlContext.UptickUserSmsCount(apiUser);
            if (upticked == false) SqlDebugger.Instance.ServerWrite("Failed to log sms uptick, userid: " + apiUser.UserId);
            return upticked;
        }
    }
}