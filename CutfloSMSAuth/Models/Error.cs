using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CutfloSMSAuth.Models
{
    public class Error
    {
        public string ErrorMessage { get; set; }
        public int ErrorCode { get; set; }

        public Error()
        {

        }

        public Error(int errorCode, string errorMessage)
        {
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
        }
    }
}
