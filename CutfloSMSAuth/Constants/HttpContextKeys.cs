using System;
namespace CutfloSMSAuth.Constants
{
    public class HttpContextKeys
    {
        public static string Phone { get; } = "PhoneNumber";
        public static string Email { get; } = "Email";
        public static string Token = "Token";
    }
}