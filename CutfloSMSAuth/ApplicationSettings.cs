using System;

public class ApplicationSettings
{
    public static string TwilioAccountSid { get; internal set; }
    public static string TwilioAuthToken { get; internal set; }
    public static string TwilioPhoneNumber { get; internal set; }
    public static string DefaultName { get; internal set; }
    public static string DefaultNumber { get; internal set; }
    public static string MailGunKey { get; internal set; }
    public static string MailGunBaseUrl { get; internal set; }

    public static string GetConnectionString()
    {
        return "yourAzureConnectionString";
    }

    public ApplicationSettings()
	{
        TwilioAccountSid = "yourTwilioSid";
        TwilioAuthToken = "yourTwilioAuthToken";
        TwilioPhoneNumber = "yourTwilioPhoneNumber";
        DefaultName = "Default";
        DefaultNumber = "+12345678900";
        MailGunKey = "yourMailGunKey";
        MailGunBaseUrl = "yourMailGunBaseUrl";
	}

    
}
