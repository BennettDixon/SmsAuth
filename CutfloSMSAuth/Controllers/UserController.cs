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

        // possibly add authentication or more security so random people cant post phone numbers and check if they are their user
        // Useful for checking if user exists after login entered before advancing pages, caution with security however.
        [Route("api/user/exists")]
        [HttpPost]
        public async Task<IActionResult> UserExists([FromBody] User post)
        {
            SqlDebugger.Instance.ServerWrite("api/user/exists endpoint is being triggered");

            User user = null;
            var _phoneNum = post.PhoneNumber;
            //Send by phone number if it is available. PRIORITY: 1.
            if (post.PhoneNumber != null)
            {
                user = await sqlContext.GetUserByPhoneAsync(post.PhoneNumber);
            }
            else if (post.Email != null && post.PhoneNumber == null)
            {
                user = await sqlContext.GetUserByEmailAsync(post.Email);
            }

            if (user == null)
            {
                return Json(GenericJsonSuccess.False);
            }
            else
            {
                return Json(GenericJsonSuccess.True);
            }

        }

        // if you want to load refresh a user and already have their login session, just load
        // if you use in large scale applications more security would be required
        [Route("/api/user/load")]
        [HttpPost]
        public async Task<IActionResult> LoadUser([FromBody] User post)
        {
            var loginSession = post.LoginSession;
            if (loginSession == null)
            {
                return Json(null);
            }
            var user = await sqlContext.GetUserFromSessionAsync(loginSession);
            return Json(user);
        }

        // Registration Methods \\

        // REQUIRED ATTRIBUTES FOR THIS POST OBJECT : User.ApiKey, User.Email OR User.PhoneNumber
        // OPTIONAL : User.FirstName
        // NOTE VALIDATE USER EXISTS IN DATABASE BY THEIR CONTACT METHOD BEFORE USING THIS ENDPOINT
        [Route("api/user/login/validate")]
        [HttpPost]
        public async Task<IActionResult> ValidatePhone([FromBody] User post)
        {
            User currentUserInDb;
            bool isEmail;

            // if phone is null && email is not null
            if (string.IsNullOrEmpty(post.PhoneNumber) && !string.IsNullOrEmpty(post.Email))
            {
                currentUserInDb = await sqlContext.GetUserByEmailAsync(post.Email);
                isEmail = true;
            }
            else
            {
                currentUserInDb = await sqlContext.GetUserByPhoneAsync(post.PhoneNumber);
                isEmail = false;
            }

            if (currentUserInDb == null) return Json(ErrorMessages.ContactMethodError());

            string email = post.Email;
            string phoneNumber = post.PhoneNumber;
            var jsonFalse = Json(new { success = false });

            if ((phoneNumber == null && email == null))
            {
                return jsonFalse;
            }

            /////////\\\\\\\\\\
            // SEND THE TOKEN \\\
            //\\\\\\\\\//////////\\
            // Send via phone
            if (isEmail == false)
            {
                string _token = currentUserInDb.SendSmsToken();

                // sending token failed
                if (string.IsNullOrEmpty(_token))
                {
                    SqlDebugger.Instance.ServerWrite(_token);
                    return Json(ErrorMessages.TokenSendingFailureError());
                }

                HttpContext.Session.SetString(PhoneKey, phoneNumber);
                HttpContext.Session.SetString(TokenKey, _token);
                return Json(GenericJsonSuccess.True);
            }
            // Send via email if no phone
            else if (isEmail == true) 
            {
                bool isRegistration = false;
                string _token = currentUserInDb.SendEmailToken(isRegistration);

                // sending token failed
                if (string.IsNullOrEmpty(_token))
                {
                    return Json(ErrorMessages.TokenSendingFailureError());
                }

                HttpContext.Session.SetString(EmailKey, email);
                HttpContext.Session.SetString(TokenKey, _token);
                return Json(GenericJsonSuccess.True);
            }

            return Json(GenericJsonSuccess.False);
        }

        // REQUIRED ATTRIBUTES FOR THIS POST OBJECT : User.ApiKey, User.Token
        [Route("api/user/login/auth/")]
        [HttpPost]
        public async Task<IActionResult> Auth([FromBody] User post)
        {
            User currentUserInDb;
            // check if our user is in the database
            if (string.IsNullOrEmpty(post.PhoneNumber) && string.IsNullOrEmpty(post.Email))
            {
                currentUserInDb = await sqlContext.GetUserByEmailAsync(post.Email);
            }
            else
            {
                currentUserInDb = await sqlContext.GetUserByPhoneAsync(post.PhoneNumber);
            }

            if (currentUserInDb == null) return Json(ErrorMessages.ContactMethodError());

            string token = post.Token;

            // check if token is same as the one we sent them, if it is, proceed with login.
            if (token == HttpContext.Session.GetString(TokenKey))
            {
                HttpContext.Session.Remove(TokenKey);
                HttpContext.Session.Remove(PhoneKey);
                string _sessionId = KeyGeneration.GenerateSession();
                currentUserInDb.LoginSession = _sessionId;
                await sqlContext.SetLoginSessionIdAsync(currentUserInDb);
                return Json(currentUserInDb);
            }
            return Json(null);
        }

        // REQUIRED ATTRIBUTES FOR THIS POST OBJECT :  User.ApiKey, User.Email OR User.PhoneNumber
        // NOTE YOU SHOULD CHECK TO SEE IF THE USER EXISTS IN YOUR DATABASE BY THEIR CONTACT METHOD BEFORE USING THIS ENDPOINT
        [Route("api/user/register/validate")]
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
            }

            // Send via phone
            if (phoneNumber != null)
            {
                string _token = user.SendSmsToken();
                
                //token sending failed
                if (string.IsNullOrEmpty(_token))
                {
                    return Json(ErrorMessages.TokenSendingFailureError());
                }

                user.Token = _token;
            }
            // Send via email if there is no phone
            else if (email != null && phoneNumber == null)
            {
                bool isRegistration = true;
                string _token = user.SendEmailToken(isRegistration);
                
                // token sending failed
                if (string.IsNullOrEmpty(_token))
                {
                    return Json(ErrorMessages.TokenSendingFailureError());
                }

                user.Token = _token;
            }

            var respBool = await sqlContext.CreateTempUserAsync(user);

            if (respBool == false)
            {
                return (Json(ErrorMessages.TempUserCreateFailedError()));
            }

            return Json(GenericJsonSuccess.True);

        }

        // REQUIRED ATTRIBUTES FOR THIS POST OBJECT : User.Token
        [Route("api/user/register/auth/")]
        [HttpPost]
        public async Task<IActionResult> RegAuth([FromBody] User post)
        {
            string token = post.Token;
            User user = await sqlContext.GetTempUserFromTokenAsync(token);

            // if token is authenticated set reg session
            if (user != null && token == user.Token)
            {
                await sqlContext.SetRegistrationSessionAsync(user);
                return Json(new SessionResponse(user.RegistrationSession));
            }
            // registration session will be null since it wasn't set
            return Json(new SessionResponse(user.RegistrationSession));
        }

        // Required attributes for User posted object: User.RegistrationSession
        [Route("api/user/register/create")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] User postedUser)
        {
            var user = await sqlContext.GetUserFromSessionAsync(postedUser.RegistrationSession, UserSqlContext.SmsRegistrationTable);

            if (user == null)
            {
                return Json(null);
            }

            bool userCreated = await sqlContext.CreateUserAsync(postedUser.FirstName, postedUser.LastName, postedUser.Email, postedUser.PhoneNumber);

            if (userCreated)
            {
                // Delete user from reg DB
                var _resp = await sqlContext.DeleteTempUserAsync(user);

                if (_resp == false)
                {
                    return Json(GenericJsonSuccess.False);
                }

                User _user;
                // refresh info from sql database to pull info like userid 
                if (postedUser.Email != null && postedUser.PhoneNumber == null)
                {
                    _user = await sqlContext.GetUserByEmailAsync(postedUser.Email);
                }
                else
                {
                    _user = await sqlContext.GetUserByPhoneAsync(postedUser.PhoneNumber);
                }
                // Set login session
                var loginSession = await sqlContext.SetLoginSessionIdAsync(_user);

                if (string.IsNullOrEmpty(loginSession))
                {
                    return Json(ErrorMessages.SessionSetError());
                }
                
                return Json(_user);
            }
            return Json(null);

        }
    }
}