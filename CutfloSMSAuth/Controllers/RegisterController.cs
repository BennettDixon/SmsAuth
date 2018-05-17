using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CutfloSMSAuth.Models;
using RestSharp;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CutfloSMSAuth.Controllers
{
    public class RegisterController : Controller
    {
        private readonly UserContext entityContext;
        private readonly UserSqlContext sqlContext;

        public RegisterController(UserContext _entityContext, UserSqlContext _sqlContext)
        {
            entityContext = _entityContext;
            sqlContext = _sqlContext;

            //Populate entity database with initial user.
            if (entityContext.Users.Count() == 0)
            {
                var user = new User()
                {
                    FirstName = ApplicationSettings.DefaultName,
                    PhoneNumber = ApplicationSettings.DefaultNumber
                };
                entityContext.Users.Add(user);
                entityContext.SaveChanges();
            }
        }

        [Route("api/user/email/register/validate")]
        [HttpPost]
        public IActionResult ValidateEmail(string email)
        {
            var user = entityContext.Users.FirstOrDefault(u => u.Email == email);

            //Temporary User 
            if (user == null)
            {
                user = new User() { Email = email };
            }

            //If there is someone to send a token to, send a token
            if (user != null)
            {
                bool isRegistration = true;

                string _token = user.SendEmailToken(isRegistration);

                HttpContext.Session.SetString("Email", user.Email);
                HttpContext.Session.SetString("Token", _token);
                entityContext.Users.Add(user);
                entityContext.SaveChanges();
                return Json(new { success = true });
            }

            return Json(new { success = false });

        }

        [Route("api/user/sms/register/validate")]
        [HttpPost]
        public IActionResult ValidateSms(string phoneNumber, string fName)
        {
            var user = entityContext.Users.FirstOrDefault(u => u.PhoneNumber == phoneNumber);

            //Temporary User 
            if (user == null)
            {
                user = new User() { PhoneNumber = phoneNumber, FirstName = fName };
            }

            //If there is someone to send a token to, send a token
            if (user != null)
            {
                string _token = user.SendSmsToken();
                HttpContext.Session.SetString("PhoneNumber", user.PhoneNumber);
                HttpContext.Session.SetString("Token", _token);
                entityContext.Users.Add(user);
                entityContext.SaveChanges();
                return Json(new { success = true });
            }

            return Json(new { success = false });

        }

        [Route("api/user/email/register/auth/")]
        [HttpPost]
        public IActionResult AuthEmail(string token)
        {
            var jsonFalse = Json(new { success = false });

            //If there is no session data, AKA a Token, return false
            if (!HttpContext.Session.Keys.Any())
            {
                return jsonFalse;
            }

            string email = HttpContext.Session.GetString("Email");

            var user = entityContext.Users.FirstOrDefault(u => u.Email == email);

            if (user != null && token == HttpContext.Session.GetString("Token"))
            {
                HttpContext.Session.Remove("Token");
                HttpContext.Session.Remove("Email");
                user.RegistrationSession = entityContext.SetRegisterSessionId(ref user);
                entityContext.Users.Update(user);
                entityContext.SaveChanges();

                return Json(new { success = true, RegistrationId = user.RegistrationSession });
            }

            return jsonFalse;
        }

        [Route("api/user/sms/register/auth/")]
        [HttpPost]
        public IActionResult AuthSms(string token)
        {
            var jsonFalse = Json(new { success = false });

            //If there is no session data, AKA a Token, return false
            if (!HttpContext.Session.Keys.Any())
            {
                return jsonFalse;
            }

            string number = HttpContext.Session.GetString("PhoneNumber");

            var user = entityContext.Users.FirstOrDefault(u => u.PhoneNumber == number);

            if (user != null && token == HttpContext.Session.GetString("Token"))
            {
                HttpContext.Session.Remove("Token");
                HttpContext.Session.Remove("PhoneNumber");
                user.RegistrationSession = entityContext.SetRegisterSessionId(ref user);
                entityContext.Users.Update(user);
                entityContext.SaveChanges();

                return Json(new { success = true, RegistrationId = user.RegistrationSession });
            }

            return jsonFalse;
        }

        [Route("api/user/sms/register/create")]
        [HttpPost]
        public IActionResult Create(string registrationId, string fName, string lName, string email, string phone, string genderPref)
        {
            var user = entityContext.Users.FirstOrDefault(u => u.RegistrationSession == registrationId);

            if (user == null)
            {
                return Json(new { Success = false });
            }

            //Internal class of UserSqlContext
            bool userCreated = sqlContext.CreateUser(fName, lName, email, phone, genderPref);

            if (userCreated)
            {
                user = sqlContext.GetUserByPhone(phone);
                var loginSession = sqlContext.SetLoginSessionId(user, out int rowsEff);

                if (rowsEff < 1) return Json(new { Error = "USER CREATED. LOGIN FAILED." });

                return Json(new { Success = true, LoginSession = loginSession });
            }
            return Json(new { Success = false });

        }
    }

}
