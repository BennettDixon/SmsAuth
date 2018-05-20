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
        public long UserId { get; set; } // FOR OUR USE
        public string CompanyName { get; set; } = "Cutflo"; //REQUIRED
        public string CompanyMailingUrl { get; set; } = "cutflo.io"; //REQUIRED
        public string ApiKey { get; set; } // REQUIRED 
        public string FirstName { get; set; } // OPTIONAL
        public string LastName { get; set; } // FOR OUR USE
        public string PhoneNumber { get; set; } // REQUIRED FOR SMS
        public string Email { get; set; } // REQUIRED FOR EMAIL
        public string Token { get; set; } // FOR OUR USE
        public string LoginSession { get; set; } // FOR OUR USE , RETURNED TO YOU FOR LOGIN AUTHENTICATION
        public string RegistrationSession { get; set; } // FOR OUR USE
        public bool IsPremium { get; set; } // FOR OUR USE
        public int SmsSent { get; set; } // FOR OUR USE

        public string SendSmsToken()
        {
            try
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
                    body: string.Format("{0}Your {1} authentication token is {2}.", name, CompanyName, token)
                );

                Console.WriteLine(message.Sid);

                return token;
            }
            catch (Exception e)
            {
                SqlDebugger.Instance.WriteError(e);
                return "0000";
            }
            
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

            string fromFormattedLine = string.Format("{0} <mailgun@{1}>", CompanyName, CompanyMailingUrl);

            request.AddParameter("domain", CompanyMailingUrl, ParameterType.UrlSegment);
            request.Resource = string.Format("{0}/messages", CompanyMailingUrl);
            request.AddParameter("from", fromFormattedLine);

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

            sb.Append(string.Format("Thanks for signing up for {0}! For your safety & security {0} doesn't use passwords. ", CompanyName));
            sb.Append("Instead, each time you need to login, an authentication token will be sent to this email. ");
            //sb.Append("If you'd like to use a phone number for this instead, click the link at the bottom of this email.\n\n");

            sb.AppendFormat("Here's your sign-up token: {0}", token);
            subject = string.Format("Confirm your {0} Account!", CompanyName);
            return sb.ToString();
        }

        private string GenerateLoginEmailBody(string token, out string subject)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Hey there!\n\n");

            sb.Append(string.Format("As always, thanks for using {0}!\n\n", CompanyName));

            sb.AppendFormat("Here's your login token: {0}", token);
            subject = string.Format("Login to {0}!", CompanyName);
            return sb.ToString();
        }
    }
}
