using System;
namespace CutfloSMSAuth
{
    public class GenericJsonSuccess
    {
        public static GenericJsonSuccess False = new GenericJsonSuccess(false);
        public static GenericJsonSuccess True = new GenericJsonSuccess(true);

        public bool Success { get; set; }

        public GenericJsonSuccess()
        {
            Success = false;
        }

        public GenericJsonSuccess(bool success)
        {
            Success = success;
        }
    }
}