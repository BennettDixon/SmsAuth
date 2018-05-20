
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
        public const string UsersTable = "cutfloUsers";
        public const string RegTable = "cutfloReg";
        public const string DebugTable = "cutfloDebug";
        public const string PremiumTable = "smsAuthPremiumUsers";
        public const string UserInfoTable = "userInfo";
        public const string ArticlesTable = "cutfloArticles";
        public const string UserTableName = "smsAuthUsers";
        public const string SmsRegistrationTable = "smsRegUsers";

        private const string MailingUrlKey = "MAILINGURL";
        private const string TokenKey = "TOKEN";
        private const string LoginSessKey = "LOGIN_SESSION";
        private const string UserIdKey = "USERID";
        private const string RegSessKey = "REGISTRATIONID";
        private const string PremiumKey = "IS_PREMIUM";
        private const string PremiumDateKey = "PREMIUM_END_DATE";
        private const string AdFreeKey = "AD_FREE";
        private const string ProfilePhotoKey = "PROFILE_PHOTO";
        private const string GenderTargetKey = "TARGETGENDER";
        private const string ApiKey = "APIKEY";
        private const string SmsCounterKey = "SMSCOUNTER";


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

        /*
        public bool SetUpgradeStatus(User user, string tableName = PremiumTable, bool wouldUpgrade = true)
        {
            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    connection.Open();
                    int rowsEff = SetUpgradeSubscription(connection, user.UserId, wouldUpgrade, tableName);
                    if (rowsEff > 0)
                        return true;
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }*/

        // Temp Registration DB Methods \\ (replacing entitycontext b/c of scalability 
        //                                  / tracking who doesn't finish signups)

        // TODO AddUser, RemoveUser, SetUserRegSession, SetUserToken,

        // Get User Methods \\
        public User GetUserByPhone(string _phoneNumber, string tableName = UserTableName)
        {
            if (SqlSecurity.ContainsIllegals(_phoneNumber)) return null;

            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    connection.Open();

                    string phoneType = string.Empty;

                    if (tableName == UserTableName)
                    {
                        phoneType = "PHONE";
                    }
                    else if (tableName == RegTable)
                    {
                        phoneType = "PHONENUMBER";
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("SELECT * FROM {0} WHERE {1} = '{2}';", tableName, phoneType, _phoneNumber);
                    string sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        User returnUser = null;

                        // Normal Users Table
                        if (tableName == UsersTable)
                        {
                            returnUser = GetUserFromReader(reader);
                        }
                        // Registration Table
                        else if (tableName == RegTable)
                        {
                            returnUser = GetTempUserFromReader(reader);
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
                SqlDebugger.Instance.WriteError(e);
                return null;
            }
        }

        public User GetUserByEmail(string _email, string tableName = UserTableName)
        {
            if (SqlSecurity.ContainsIllegals(_email))
            {
                return null;
            }

            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    connection.Open();

                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("SELECT * FROM {0} WHERE EMAIL = '{1}';", tableName, _email);
                    string sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        User returnUser = null;

                        if (tableName == UsersTable)
                        {
                            returnUser = GetUserFromReader(reader);
                        }
                        else if (tableName == RegTable)
                        {
                            returnUser = GetTempUserFromReader(reader);
                        }

                        return returnUser;
                    }

                }
            }
            catch (SqlException e)
            {
                SqlDebugger.Instance.ServerWrite(e.Message);
                return null;
            }
        }

        /*
        public User GetUserFromSession(string session, string tableName = UsersTable)
        {
            if (SqlSecurity.ContainsIllegals(session)) return null;

            using (SqlConnection connection = GetConnection())
            {
                connection.Open();
                string sessionKey = string.Empty;

                if (tableName == UsersTable)
                {
                    sessionKey = LoginSessKey;
                }
                else if (tableName == RegTable)
                {
                    sessionKey = RegSessKey;
                }

                string sql = string.Format("SELECT * FROM {0} WHERE {1} = '{2}'", tableName, sessionKey, session);
                using (SqlCommand command = new SqlCommand(sql, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    User user = null;

                    if (tableName == UsersTable)
                    {
                        user = GetUserFromReader(reader);
                    }
                    else if (tableName == RegTable)
                    {
                        user = GetTempUserFromReader(reader);
                    }
                    return user;
                }
            }
        }*/

        public User GetTempUserFromToken(string token, string tableName = SmsRegistrationTable)
        {
            if (SqlSecurity.ContainsIllegals(token)) return null;

            using (SqlConnection connection = GetConnection())
            {
                connection.Open();
                string sql = string.Format("SELECT * FROM {0} WHERE {1} = '{2}'", tableName, TokenKey, token);
                using (SqlCommand command = new SqlCommand(sql, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    User user = null;
                    user = GetTempUserFromReader(reader);
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

        // Login Methods \\
        /*public async Task<User> GetAdditionalUserInfo(User user, string tableName = UserInfoTable)
        {
            try
            {
                string sql = string.Format("SELECT * FROM {0} WHERE {1} = '{2}'", tableName, UserIdKey, user.UserId);
                using (SqlConnection connection = GetConnection())
                {
                    await connection.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            var _profilePhotoPath = reader.IsDBNull(1) ? null : reader.GetString(1);
                            reader.Close();
                            user.ProfilePhotoPath = _profilePhotoPath;
                        }
                        return user;
                    }
                }
            }
            catch (Exception ex)
            {
                SqlDebugger.Instance.SetDebugContext(user.UserId);
                SqlDebugger.Instance.ServerWrite("Ex Caught:" + ex.Message);
                return user;
            }
        }*/

        public async Task<bool> UptickUserSmsCount(User user, string tableName = UserTableName)
        {
            try
            {
                string sql = string.Format("UPDATE {0} SET {1} = '{2}' WHERE {3} = '{4}';", tableName, SmsCounterKey, user.SmsSent + 1, ApiKey, user.ApiKey);
                using (SqlConnection connection = GetConnection())
                {
                    await connection.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        int resp = await cmd.ExecuteNonQueryAsync();
                        if (resp > 0)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    
                }
            }
            catch (Exception ex)
            {
                await SqlDebugger.Instance.WriteErrorAsync(ex);
                return false;
            }
        }

        /*
        public User CheckPremium(User user, string tableName = PremiumTable)
        {
            try
            {
                string sql = string.Format("SELECT * FROM {0} WHERE {1} = '{2}'", tableName, UserIdKey, user.UserId);
                using (SqlConnection connection = GetConnection())
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int _IsPremium = reader.IsDBNull(1) ? 0 : reader.GetInt32(2);
                            SqlDateTime _expiryDate = reader.IsDBNull(3) ? SqlDateTime.MinValue : reader.GetSqlDateTime(3);
                            if (_IsPremium == 0) user.IsPremium = false;
                            else if (_IsPremium == 1) user.IsPremium = true;
                            reader.Close();

                            int _expiryComparison = _expiryDate.CompareTo(DateTime.UtcNow);
                            // If expiry date is exactly now, or later it will return 0 or 1
                            if (_IsPremium == 1 && _expiryComparison <= 0)
                            {
                                SetUpgradeSubscription(connection, user.UserId, false, tableName);
                                user.IsPremium = false;
                            }
                        }
                        return user;
                    }
                }
            }
            catch (Exception ex)
            {
                SqlDebugger.Instance.ServerWrite("Ex Caught:" + ex.Message);
                return user;
            }
        }
        */

            /*
        private int SetUpgradeSubscription(SqlConnection connection, long UserId, bool isUpgrade, string tableName = PremiumTable)
        {
            try
            {
                string sqlInsert = string.Format("INSERT INTO {0} ({1}) VALUES({2})", tableName, UserIdKey, UserId);
                using (SqlCommand cmd = new SqlCommand(sqlInsert, connection))
                {
                    int rowsEff = cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Already In Premium Table");
            }

            int? _boolToInt = null;
            if (isUpgrade == true)
                _boolToInt = 1;
            else if (isUpgrade == false)
                _boolToInt = 0;

            DateTime oneMonthFromNow = DateTime.UtcNow.AddMonths(1);

            string sql = string.Format("UPDATE {0} SET {1} = '{2}' WHERE {3} = '{4}'", tableName, PremiumKey, _boolToInt, UserIdKey, UserId);
            string sqlCmd2 = string.Format("UPDATE {0} SET {1} = '{2}' WHERE {3} = '{4}'", tableName, PremiumDateKey, oneMonthFromNow, UserIdKey, UserId);
            using (SqlCommand cmd = new SqlCommand(sql, connection))
            using (SqlCommand cmd2 = new SqlCommand(sqlCmd2, connection))
            {
                int rowsEff = cmd.ExecuteNonQuery();
                if (isUpgrade == false)
                {
                    return rowsEff;
                }
                rowsEff += cmd2.ExecuteNonQuery();
                return rowsEff;
            }
        }
        */

        public async Task<string> SetLoginSessionId(User user, string tableName = UserTableName)
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

        public async Task<string> SetRegistrationSession(User user, string tableName = SmsRegistrationTable)
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

        // Registration Methods \\
        public bool CreateUser(string fName, string lName, string email, string phone, string genderPref)
        {
            string[] sqlStrs = { fName, lName, email, phone, genderPref };
            if (SqlSecurity.BatchContainsIllegals(sqlStrs))
            {
                return false;
            }

            // Trim strings to make sure no leading whitespace
            //FileSystemParsers.RemoveSpacesFromBatch(sqlStrs);

            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    connection.Open();

                    StringBuilder sb = new StringBuilder();
                    sb.Append("INSERT INTO cutfloUsers (FNAME, LNAME, EMAIL, PHONE, GENDER_PREF)");
                    sb.AppendFormat("VALUES ('{0}', '{1}', '{2}', '{3}', '{4}');", fName, lName, email, phone, genderPref);
                    string sql = sb.ToString();

                    using (SqlCommand createUser = new SqlCommand(sql, connection))
                    {
                        int rowsEff = createUser.ExecuteNonQuery();

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
                SqlDebugger.Instance.WriteError(ex);
                return false;
            }
        }

        // cutfloReg DB
        public async Task<bool> CreateTempUser(User user, string tableName = SmsRegistrationTable)
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

        public bool DeleteTempUser(User user, string tableName = SmsRegistrationTable)
        {
            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    connection.Open();

                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("DELETE FROM {0}", tableName);
                    sb.AppendFormat(" WHERE USERID = '{0}'", user.UserId);
                    string sql = sb.ToString();

                    using (SqlCommand deleteUser = new SqlCommand(sql, connection))
                    {

                        int rowsEff = deleteUser.ExecuteNonQuery();

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
                SqlDebugger.Instance.WriteError(ex);
                return false;
            }
        }

        public async Task<User> AuthApiUser(string apiToken, string tableName = UserTableName)
        {
            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    await connection.OpenAsync();
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("SELECT * FROM {0} WHERE {1} = '{2}';", tableName, ApiKey, apiToken);
                    string sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        var apiUser = GetUserFromReader(reader);
                        return apiUser;
                    }
                }
            }
            catch (SqlException e)
            {
                await SqlDebugger.Instance.WriteErrorAsync(e);
                return null;
            }
        }


    }
}

       