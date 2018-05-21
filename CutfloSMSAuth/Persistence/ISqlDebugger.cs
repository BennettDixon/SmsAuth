using System;
using System.Threading.Tasks;
using CutfloSMSAuth.Models;

namespace CutfloSMSAuth
{
    public interface ISqlDebugger
    {
        void ServerWrite(string logMsg, string tableName = UserSqlContext.UserTableName);
        Task ServerWriteAsync(string logMst, string tableName = UserSqlContext.UserTableName);
        void SetDebugContext(long context);
        void WriteError(Exception e);
        Task WriteErrorAsync(Exception e);
    }
}