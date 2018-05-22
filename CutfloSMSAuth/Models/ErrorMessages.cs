using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CutfloSMSAuth.Models
{
    public class ErrorMessages
    {
        public static Error ApiAuthenticationError()
        {
            return new Error(500, "APIAuthenticationFailed");
        }
        public static Error ContactMethodError()
        {
            return new Error(303, "ContactMethodProvidedInvalid");
        }
        public static Error TokenSendingFailureError()
        {
            return new Error(202, "TokenFailedToSend");
        }
        public static Error TempUserCreateFailedError()
        {
            return new Error(103, "TempUserCreateFailed");
        }
        public static Error SessionSetError()
        {
            return new Error(101, "SesssionFailedToSet");
        }
    }
}
