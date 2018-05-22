
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
        public const string UserTableName = "smsAuthUsers";
        public const string SmsRegistrationTable = "smsRegUsers";

        private const string FirstNameKey = "FIRSTNAME";
        private const string LastNameKey = "LASTNAME";
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

        // constructor, initialize sql connection string and debugger, debugger is depreciated as an interface, see below.
        // use SqlDebugger.Instance.ServerWriteAsync(string msg);
        public UserSqlContext(ISqlDebugger _debugger)
        {
            Debugger = _debugger;
            ConnectionString = ApplicationSettings.GetConnectionString();
        }

        // Return a new SQL connection
        private SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        // depreceated since we use SqlDebugger.Instance.ServerWriteAsync(msg) now,
        // but if you want to use it you can, its implemented with an interface properly and makes compile time smaller
        // its just not quite as easy to access as a static instance across the entire application
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
        // grab user by phone, careful not very secure
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

        // grab the user by email, careful not very secure
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

        // gets a user from the user table or registration table (user is default) based on reg/login session
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

        // Grabs user from registration table in the registration/auth endpoint from a token that is posted
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

        // CHANGE/ADD/SUBTRACT TO/FROM BASED ON YOUR USER DATA STRUCTURE NEEDS
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


        // CHANGE/ADD/SUBTRACT TO/FROM BASED ON YOUR USER DATA STRUCTURE NEEDS
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


        // DO NOT CHANGE UNLESS YOU SETUP YOUR REGISTRATION TABLE WITH A DIFFERENT SQL STATEMENT THEN WE DID IN THE TUTORIAL
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

        // DO NOT CHANGE UNLESS YOU SETUP YOUR REGISTRATION TABLE WITH A DIFFERENT SQL STATEMENT THEN WE DID IN THE TUTORIAL
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

        // grab a user from their id, useful and fast when we already know the user we are dealing with has been secured and validated
        private int GetUserId(SqlConnection connection, string session, string tableName = UserTableName)
        {
            if (SqlSecurity.ContainsIllegals(session)) return -1;

            string sql = string.Format("SELECT {0} FROM {1} WHERE {2} = '{3}'", UserIdKey, tableName, LoginSessKey, session);
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

        // used to create a user, no security in this function itself, just the controller, be careful.
        public async Task<bool> CreateUserAsync(string fName, string lName, string email, string phone, string tableName = UserTableName)
        {
            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    await connection.OpenAsync();

                    StringBuilder sb = new StringBuilder();
                    // alter this sql statement to create whatever attributes you need for your user, inject through the function arguments
                    sb.AppendFormat("INSERT INTO {0} ({1}, {2}, {3}, {4})", tableName, FirstNameKey, LastNameKey, EmailSqlKey, PhoneSqlKey);
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

        // used after login is validated, no security in this other than simple sql illegals, validation should be implemented in controller
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
                string sql = string.Format("UPDATE {0} SET {1} = '{2}' WHERE {3} = {4}", tableName, LoginSessKey, _sessionId, UserIdKey, user.UserId);
                using (SqlCommand insertSession = new SqlCommand(sql, connection))
                {
                    await insertSession.ExecuteNonQueryAsync();
                    return _sessionId;
                }
            }
        }

        // used after registration is validated and before creation, no security in this other than simple sql illegals, validation should be implemented in controller
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
                string sql = string.Format("UPDATE {0} SET {1} = '{2}' WHERE {3} = {4}", tableName, RegSessKey, _sessionId, UserIdKey, user.UserId);
                using (SqlCommand insertSession = new SqlCommand(sql, connection))
                {
                    await insertSession.ExecuteNonQueryAsync();
                    return _sessionId;
                }
            }
        }

        // create a temporary registration user with their contact method and token, not very secure, implement validation in controller.
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
                    sb.AppendFormat("INSERT INTO {0} ({1}, {2}, {3}, {4})", tableName, RegSessKey, TokenKey, EmailSqlKey, PhoneSqlKey);
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

        // used after user is created from reg table and moved to user table, cleans out old reg users that don't need to be in the reg table anymore
        // as always validate in the controller before use
        public async Task<bool> DeleteTempUserAsync(User user, string tableName = SmsRegistrationTable)
        {
            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    await connection.OpenAsync();

                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("DELETE FROM {0}", tableName);
                    sb.AppendFormat(" WHERE {0} = '{1}'", UserIdKey, user.UserId);
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

       