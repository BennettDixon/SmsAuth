using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CutfloSMSAuth.Models
{
    public class SessionResponse
    {
        public string Session { get; set; }

        public SessionResponse()
        {
            Session = null;
        }

        public SessionResponse(string session)
        {
            Session = session;
        }
    }
}
