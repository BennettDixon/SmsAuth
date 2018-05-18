using System;
using CutfloSMSAuth.Models;

namespace CutfloSMSAuth
{
    public interface ISqlDebugger
    {
        void ServerWrite(string logMsg);
        void SetDebugContext(long context);
    }
}