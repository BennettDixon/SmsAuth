using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using RestSharp;
using RestSharp.Authenticators;

namespace CutfloSMSAuth.Models
{
    public class User
    {
        public long UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string GenderPref { get; set; }
        public string LoginSession { get; set; }
        public string RegistrationSession { get; set; }

        public string SendSmsToken()
        {
            var _sid = ApplicationSettings.TwilioAccountSid;
            var _token = ApplicationSettings.TwilioAuthToken;
            var _fromNumber = ApplicationSettings.TwilioPhoneNumber;

            TwilioClient.Init(_sid, _token);

            var toNumber = new PhoneNumber(PhoneNumber);
            var fromNumber = new PhoneNumber(_fromNumber);

            string token = GenerateToken();

            string name = (FirstName != null) ? string.Format("Hey {0}! ", FirstName) : "";

            var message = MessageResource.Create(
                toNumber,
                from: fromNumber,
                body: string.Format("{0}Your Cutflo authentication token is {1}.", name, token)
            );

            Console.WriteLine(message.Sid);

            return token;
        }

        public string SendEmailToken(bool isSignUp)
        {
            string token = GenerateToken();
            string body = "Error Generating Body";
            string subject = "Error Generating Subject";

            if (isSignUp)
            {
                body = GenerateSignUpEmailBody(token, out subject);
            }
            else
            {
                body = GenerateLoginEmailBody(token, out subject);
            }

            string _apiKey = ApplicationSettings.MailGunKey;
            string _apiBaseUrl = ApplicationSettings.MailGunBaseUrl;
            RestClient client = new RestClient
            {
                BaseUrl = new Uri(_apiBaseUrl),
                Authenticator = new HttpBasicAuthenticator("api", _apiKey)
            };
            RestRequest request = new RestRequest();

            request.AddParameter("domain", "cutflo.io", ParameterType.UrlSegment);
            request.Resource = "cutflo.io/messages";
            request.AddParameter("from", "Cutflo <mailgun@cutflo.io>");

            // NOTE "Email" parameter is bound to THIS CLASS AND IS A FIELD. SO IT subject (out var of GenerateEmailBody)
            request.AddParameter("to", Email);
            request.AddParameter("subject", subject);
            request.AddParameter("text", body);

            request.Method = Method.POST;
            var resp = client.Execute(request);
            var error_log = resp.Content;

            //return token;
            return error_log;
        }

        private string GenerateToken()
        {
            // Generate four-digit token
            var r = new Random((int)DateTime.Now.Ticks);
            string token = r.Next(1000, 9999).ToString();
            return token;
        }

        private string GenerateSignUpEmailBody(string token, out string subject)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Hey there!\n\n");

            sb.Append("Thanks for signing up for Cutflo! For your safety & security Cutflo doesn't use passwords. ");
            sb.Append("Instead, each time you need to login, an authentication token will be sent to this email. ");
            //sb.Append("If you'd like to use a phone number for this instead, click the link at the bottom of this email.\n\n");

            sb.AppendFormat("Here's your sign-up token: {0}", token);
            subject = "Confirm your Cutflo Account!";
            return sb.ToString();
        }

        private string GenerateLoginEmailBody(string token, out string subject)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Hey there!\n\n");

            sb.Append("As always, thanks for using Cutflo!\n\n");

            sb.AppendFormat("Here's your login token: {0}", token);
            subject = "Login to Cutflo!";
            return sb.ToString();
        }
    }
}
