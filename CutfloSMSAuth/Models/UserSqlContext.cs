using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CutfloSMSAuth;
using Microsoft.Extensions.DependencyInjection;

namespace CutfloSMSAuth.Models
{
    public class UserSqlContext
    {
        public string ConnectionString { get; set; }

        public UserSqlContext()
        {
            ConnectionString = ApplicationSettings.GetConnectionString();
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        // Get User Methods \\
        public User GetUserByPhone(string _phoneNumber)
        {
            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    connection.Open();

                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("SELECT * FROM cutfloUsers WHERE PHONE = '{0}';", _phoneNumber);
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            var returnUser = GetUserFromReader(reader);

                            if (returnUser.FirstName != null && returnUser.PhoneNumber != null)
                            {
                                return returnUser;
                            }
                            else return null;
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public User GetUserByEmail(string _email)
        {
            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    connection.Open();

                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("SELECT * FROM cutfloUsers WHERE EMAIL = '{0}';", _email);
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            var returnUser = GetUserFromReader(reader);

                            if (returnUser.FirstName != null && returnUser.Email != null)
                            {
                                return returnUser;
                            }
                            else return null;
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        private User GetUserFromReader(SqlDataReader reader)
        {
            if (reader.Read())
            {
                var user = new User
                {
                    UserId = reader.IsDBNull(0) ? -1 : reader.GetInt32(0),
                    FirstName = reader.IsDBNull(1) ? null : reader.GetString(1),
                    LastName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                    PhoneNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
                    GenderPref = reader.IsDBNull(5) ? null : reader.GetString(5),
                    LoginSession = reader.IsDBNull(6) ? null : reader.GetString(6)
                };
                return user;
            }
            return null;
        }

        // Login Methods \\
        public string SetLoginSessionId(User user, out int rowsEff)
        {
            if (user == null)
            {
                rowsEff = 0;
                return "ERROR: User is null.";
            }
            using (SqlConnection connection = GetConnection())
            {
                connection.Open();
                // Set user's session column to string sessionId and return so we can return Json
                var _sessionId = KeyGeneration.GenerateSession();
                user.LoginSession = _sessionId;
                SqlCommand insertSession = new SqlCommand(string.Format("UPDATE cutfloUsers SET LOGIN_SESSION = '{0}' WHERE USERID = {1}", _sessionId, user.UserId), connection);

                rowsEff = insertSession.ExecuteNonQuery();
                return _sessionId;
            }
        }

        // Registration Methods \\
        public bool CreateUser(string fName, string lName, string email, string phone, string genderPref)
        {
            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    connection.Open();

                    StringBuilder sb = new StringBuilder();
                    sb.Append("INSERT INTO cutfloUsers (FNAME, LNAME, EMAIL, PHONE, GENDER_PREF)");
                    sb.AppendFormat("VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", fName, lName, email, phone, genderPref);
                    String sql = sb.ToString();

                    SqlCommand createUser = new SqlCommand(sql, connection);

                    int rowsEff = createUser.ExecuteNonQuery();

                    if (rowsEff > 0)
                    {
                        return true;
                    }
                    return false;
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        // Cut Methods \\
        public List<Cut> GetCutsBySession(string session)
        {
            List<Cut> returnCuts = new List<Cut>();
            List<int> cutIds = new List<int>();

            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    string sql;

                    connection.Open();

                    int curCuts = GetCurrentNumCuts(connection, session);

                    //Grab each cut ID for the user based on their login session so we can lookup in cutsTable
                    for (int i = 0; i < curCuts; i++)
                    {
                        var cutColumn = string.Format("CUT_{0}", i);
                        sql = string.Format("SELECT {0} FROM cutfloUsers WHERE LOGIN_SESSION = '{1}'", cutColumn, session);

                        SqlCommand getCutTableId = new SqlCommand(sql, connection);
                        int cutId = (int)getCutTableId.ExecuteScalar();
                        cutIds.Add(cutId);
                    }


                    //Grab each cut out of the cutTable by cutID
                    foreach (int cutId in cutIds)
                    {
                        sql = string.Format("SELECT * FROM cutfloCuts WHERE CUT_ID = '{0}'", cutId);

                        SqlCommand getCut = new SqlCommand(sql, connection);

                        using (SqlDataReader reader = getCut.ExecuteReader())
                        {
                            Cut cut = GetCut(reader);
                            returnCuts.Add(cut);
                        }
                    }
                    return returnCuts;
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        private int GetCurrentNumCuts(SqlConnection connection, string session)
        {
            try
            {
                int curCuts = -1;

                string sql = string.Format("SELECT CUR_CUTS FROM cutfloUsers WHERE LOGIN_SESSION = '{0}'", session);

                SqlCommand getCurCuts = new SqlCommand(sql, connection);

                curCuts = (int) getCurCuts.ExecuteScalar();

                return curCuts;
            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex);
                return -1;
            }
        }

        private int GetAllowedCuts(SqlConnection connection, string session)
        {
            try
            {
                int maxCuts = -1;

                string sql = string.Format("SELECT MAX_CUTS FROM cutfloUsers WHERE LOGIN_SESSION = '{0}'", session);

                SqlCommand getCutTableId = new SqlCommand(sql, connection);

                using (SqlDataReader reader = getCutTableId.ExecuteReader())
                {
                    maxCuts = reader.GetInt32(0);
                }

                return maxCuts;
            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex);
                return -1;
            }
        }

        //Grab Cut Data from cutTable when given reader executing SELECT * FROM cutfloCuts WHERE CUT_ID command
        private Cut GetCut(SqlDataReader reader)
        {
            if (reader.Read())
            {
                DateTime cutDate = reader.IsDBNull(1) ? DateTime.Now : reader.GetDateTime(1);
                string cutSpin = reader.IsDBNull(2) ? null : reader.GetString(2);
                string cutName = reader.IsDBNull(3) ? null : reader.GetString(3);

                if (cutSpin == null)
                {
                    return null;
                }
                Cut cut = new Cut { CutDate = cutDate, CutSpin = cutSpin, CutName = cutName };
                return cut;
            }
            return null;
        }
    }
}
