
using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CutfloSMSAuth;
using Microsoft.Extensions.DependencyInjection;
using System.Data.SqlTypes;

namespace CutfloSMSAuth.Models
{
    public class UserSqlContext
    {
        public const string DebugTable = "smsAuthDebug";
        public const string PremiumTable = "smsAuthPremiumUsers";
        public const string UserTableName = "smsAuthUsers";
        public const string SmsRegistrationTable = "smsRegUsers";

        private const string PhoneSqlKey = "PHONENUMBER";
        private const string EmailSqlKey = "EMAIL";
        private const string MailingUrlKey = "MAILINGURL";
        private const string TokenKey = "TOKEN";
        private const string LoginSessKey = "LOGIN_SESSION";
        private const string UserIdKey = "USERID";
        private const string RegSessKey = "REGISTRATIONID";
        private const string AdFreeKey = "AD_FREE";
        private const string ApiKey = "APIKEY";


        public string ConnectionString { get; set; }

        private readonly ISqlDebugger Debugger;

        public UserSqlContext(ISqlDebugger _debugger)
        {
            Debugger = _debugger;
            ConnectionString = ApplicationSettings.GetConnectionString();
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        public bool WriteSqlDebug(string msg)
        {
            try
            {
                Debugger.ServerWrite(msg);
                return true;
            }
            catch (Exception ex)
            {
                Debugger.ServerWrite(ex.ToString());
                return false;
            }
        }

        // Get User Methods \\
        public async Task<User> GetUserByPhoneAsync(string _phoneNumber, string tableName = UserTableName, string phoneSqlColumnName = PhoneSqlKey)
        {
            if (SqlSecurity.ContainsIllegals(_phoneNumber)) return null;

            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    await connection.OpenAsync();

                    string phoneType = string.Empty;

                    phoneType = phoneSqlColumnName;

                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("SELECT * FROM {0} WHERE {1} = '{2}';", tableName, phoneType, _phoneNumber);
                    string sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        User returnUser = null;

                        // Normal Users Table
                        if (tableName == UserTableName)
                        {
                            returnUser = await GetUserFromReaderAsync(reader);
                        }
                        // Registration Table
                        else if (tableName == SmsRegistrationTable)
                        {
                            returnUser = await GetTempUserFromReaderAsync(reader);
                        }

                        if (returnUser == null)
                        {
                            return null;
                        }
                        if (returnUser.FirstName != null && returnUser.PhoneNumber != null)
                        {
                            return returnUser;
                        }
                        return null;
                    }
                }
            }
            catch (SqlException e)
            {
                await SqlDebugger.Instance.WriteErrorAsync(e);
                return null;
            }
        }

        public async Task<User> GetUserByEmailAsync(string _email, string tableName = UserTableName, string emailSqlColumnName = EmailSqlKey)
        {
            if (SqlSecurity.ContainsIllegals(_email))
            {
                return null;
            }

            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    await connection.OpenAsync();

                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("SELECT * FROM {0} WHERE {1} = '{2}';", tableName, emailSqlColumnName,_email);
                    string sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        User returnUser = null;

                        if (tableName == UserTableName)
                        {
                            returnUser = await GetUserFromReaderAsync(reader);
                        }
                        else if (tableName == SmsRegistrationTable)
                        {
                            returnUser = await GetTempUserFromReaderAsync(reader);
                        }
                        return returnUser;
                    }

                }
            }
            catch (SqlException e)
            {
                await SqlDebugger.Instance.WriteErrorAsync(e);
                return null;
            }
        }

        public async Task<User> GetUserFromSessionAsync(string session, string tableName = UserTableName)
        {
            if (SqlSecurity.ContainsIllegals(session)) return null;

            using (SqlConnection connection = GetConnection())
            {
                await connection.OpenAsync();
                string sessionKey = string.Empty;

                if (tableName == UserTableName)
                {
                    sessionKey = LoginSessKey;
                }
                else if (tableName == SmsRegistrationTable)
                {
                    sessionKey = RegSessKey;
                }

                string sql = string.Format("SELECT * FROM {0} WHERE {1} = '{2}'", tableName, sessionKey, session);
                using (SqlCommand command = new SqlCommand(sql, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    User user = null;

                    if (tableName == UserTableName)
                    {
                        user = await GetUserFromReaderAsync(reader);
                    }
                    else if (tableName == SmsRegistrationTable)
                    {
                        user = await GetTempUserFromReaderAsync(reader);
                    }
                    return user;
                }
            }
        }

        public async Task<User> GetTempUserFromTokenAsync(string token, string tableName = SmsRegistrationTable)
        {
            if (SqlSecurity.ContainsIllegals(token)) return null;

            using (SqlConnection connection = GetConnection())
            {
                await connection.OpenAsync();
                string sql = string.Format("SELECT * FROM {0} WHERE {1} = '{2}'", tableName, TokenKey, token);
                using (SqlCommand command = new SqlCommand(sql, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    User user = null;
                    user = await GetTempUserFromReaderAsync(reader);
                    return user;
                }
            }
        }

        private User GetUserFromReader(SqlDataReader reader)
        {
            try
            {
                if (reader.Read())
                {
                    var user = new User
                    {
                        UserId = reader.IsDBNull(0) ? -1 : reader.GetInt32(0),
                        ApiKey = reader.IsDBNull(1) ? null : reader.GetString(1),
                        CompanyName = reader.IsDBNull(2) ? null : reader.GetString(2),
                        CompanyMailingUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
                        FirstName = reader.IsDBNull(4) ? null : reader.GetString(4),
                        LastName = reader.IsDBNull(5) ? null : reader.GetString(5),
                        Email = reader.IsDBNull(6) ? null : reader.GetString(6),
                        SmsSent = reader.IsDBNull(7) ? -1 : reader.GetInt32(7),
                    };
                    return user;
                }
                return null;
            }
            catch (Exception e)
            {
                SqlDebugger.Instance.WriteError(e);
                return null;
            }
        }

        private async Task<User> GetUserFromReaderAsync(SqlDataReader reader)
        {
            try
            {
                if (await reader.ReadAsync())
                {
                    var user = new User
                    {
                        UserId = await reader.IsDBNullAsync(0) ? -1 : reader.GetInt32(0),
                        ApiKey = await reader.IsDBNullAsync(1) ? null : reader.GetString(1),
                        CompanyName = await reader.IsDBNullAsync(2) ? null : reader.GetString(2),
                        CompanyMailingUrl = await reader.IsDBNullAsync(3) ? null : reader.GetString(3),
                        FirstName = await reader.IsDBNullAsync(4) ? null : reader.GetString(4),
                        LastName = await reader.IsDBNullAsync(5) ? null : reader.GetString(5),
                        Email = await reader.IsDBNullAsync(6) ? null : reader.GetString(6),
                        SmsSent = await reader.IsDBNullAsync(7) ? -1 : reader.GetInt32(7),
                    };
                    return user;
                }
                return null;
            }
            catch (Exception e)
            {
                await SqlDebugger.Instance.WriteErrorAsync(e);
                return null;
            }
        }

        private User GetTempUserFromReader(SqlDataReader reader)
        {
            if (reader.Read())
            {
                var user = new User
                {
                    UserId = reader.IsDBNull(0) ? -1 : reader.GetInt32(0),
                    Email = reader.IsDBNull(1) ? null : reader.GetString(1),
                    PhoneNumber = reader.IsDBNull(2) ? null : reader.GetString(2),
                    RegistrationSession = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Token = reader.IsDBNull(4) ? null : reader.GetString(4)
                };
                return user;
            }

            return null;
        }

        private async Task<User> GetTempUserFromReaderAsync(SqlDataReader reader)
        {
            if (await reader.ReadAsync())
            {
                var user = new User
                {
                    UserId = await reader.IsDBNullAsync(0) ? -1 : reader.GetInt32(0),
                    Email = await reader.IsDBNullAsync(1) ? null : reader.GetString(1),
                    PhoneNumber = await reader.IsDBNullAsync(2) ? null : reader.GetString(2),
                    RegistrationSession = await reader.IsDBNullAsync(3) ? null : reader.GetString(3),
                    Token = await reader.IsDBNullAsync(4) ? null : reader.GetString(4)
                };
                return user;
            }

            return null;
        }

        private int GetUserId(SqlConnection connection, string session, string tableName = UserTableName)
        {
            if (SqlSecurity.ContainsIllegals(session)) return -1;

            string sql = string.Format("SELECT USERID FROM {0} WHERE {1} = '{2}'", tableName, ApiKey, session);
            using (SqlCommand checkExists = new SqlCommand(sql, connection))
            {
                int? userId = (int)checkExists.ExecuteScalar();
                if (userId == null)
                {
                    return -1;
                }
                return (int)userId;
            }
        }

        public async Task<bool> CreateUserAsync(string fName, string lName, string email, string phone, string tableName = UserTableName)
        {
            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    await connection.OpenAsync();

                    StringBuilder sb = new StringBuilder();
                    // alter this sql statement to create whatever attributes you need for your user, inject through the function arguments
                    sb.AppendFormat("INSERT INTO {0} (FNAME, LNAME, EMAIL, PHONE)", tableName);
                    sb.AppendFormat("VALUES ('{0}', '{1}', '{2}', '{3}')", fName, lName, email, phone);
                    String sql = sb.ToString();

                    SqlCommand createUser = new SqlCommand(sql, connection);

                    int rowsEff = await createUser.ExecuteNonQueryAsync();

                    if (rowsEff > 0)
                    {
                        return true;
                    }
                    return false;
                }
            }
            catch (SqlException ex)
            {
                await SqlDebugger.Instance.WriteErrorAsync(ex);
                return false;
            }
        }

        public async Task<string> SetLoginSessionIdAsync(User user, string tableName = UserTableName)
        {
            if (user == null)
            {
                return "ERROR: User is null.";
            }

            if (SqlSecurity.ContainsIllegals(user.UserId.ToString()))
            {
                return null;
            }

            using (SqlConnection connection = GetConnection())
            {
                await connection.OpenAsync();
                // Set user's session column to string sessionId and return so we can return Json
                var _sessionId = KeyGeneration.GenerateSession();
                user.LoginSession = _sessionId;
                string sql = string.Format("UPDATE {0} SET LOGIN_SESSION = '{1}' WHERE USERID = {2}", tableName, _sessionId, user.UserId);
                using (SqlCommand insertSession = new SqlCommand(sql, connection))
                {
                    await insertSession.ExecuteNonQueryAsync();
                    return _sessionId;
                }
            }
        }

        public async Task<string> SetRegistrationSessionAsync(User user, string tableName = SmsRegistrationTable)
        {
            if (user == null)
            {
                return "ERROR: User is null.";
            }
            if (SqlSecurity.ContainsIllegals(user.UserId.ToString()))
            {
                return "ERROR: contains illegals.";
            }

            using (SqlConnection connection = GetConnection())
            {
                await connection.OpenAsync();
                // Set user's session column to string sessionId and return so we can return Json
                var _sessionId = KeyGeneration.GenerateSession();
                user.RegistrationSession = _sessionId;
                string sql = string.Format("UPDATE {0} SET REGISTRATIONID = '{1}' WHERE USERID = {2}", tableName, _sessionId, user.UserId);
                using (SqlCommand insertSession = new SqlCommand(sql, connection))
                {
                    await insertSession.ExecuteNonQueryAsync();
                    return _sessionId;
                }
            }
        }

        // cutfloReg DB
        public async Task<bool> CreateTempUserAsync(User user, string tableName = SmsRegistrationTable)
        {
            string[] sqlStrs = { user.PhoneNumber, user.Email, user.Token };
            if (SqlSecurity.BatchContainsIllegals(sqlStrs))
            {
                return false;
            }

            try
            {
                using (SqlConnection connection = GetConnection())
                {
                   await connection.OpenAsync();

                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("INSERT INTO {0} (REGISTRATIONID, TOKEN, EMAIL, PHONENUMBER)", tableName);
                    sb.AppendFormat("VALUES ('{0}', '{1}', '{2}', '{3}');", user.RegistrationSession, user.Token, user.Email, user.PhoneNumber);
                    string sql = sb.ToString();

                    using (SqlCommand createUser = new SqlCommand(sql, connection))
                    {
                        int rowsEff = await createUser.ExecuteNonQueryAsync();

                        if (rowsEff > 0)
                        {
                            return true;
                        }
                        return false;
                    }
                }
            }
            catch (SqlException ex)
            {
                await SqlDebugger.Instance.WriteErrorAsync(ex);
                return false;
            }
        }

        public async Task<bool> DeleteTempUserAsync(User user, string tableName = SmsRegistrationTable)
        {
            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    await connection.OpenAsync();

                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("DELETE FROM {0}", tableName);
                    sb.AppendFormat(" WHERE USERID = '{0}'", user.UserId);
                    string sql = sb.ToString();

                    using (SqlCommand deleteUser = new SqlCommand(sql, connection))
                    {

                        int rowsEff = await deleteUser.ExecuteNonQueryAsync();

                        if (rowsEff > 0)
                        {
                            return true;
                        }
                        return false;
                    }
                }
            }
            catch (SqlException ex)
            {
                await SqlDebugger.Instance.WriteErrorAsync(ex);
                return false;
            }
        }
    }
}

       