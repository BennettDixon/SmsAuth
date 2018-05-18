﻿using System;
using System.Data.SqlClient;
using System.Text;

namespace CutfloSMSAuth.Models
{
    public class SqlDebugger : ISqlDebugger
    {
        public static SqlDebugger Instance { get; set; } = new SqlDebugger();

        public string ConnectionString { get; set; }
        public long DebuggerContext { get; set; } = 1;

        public SqlDebugger()
        {
            ConnectionString = ApplicationSettings.GetConnectionString();
        }
        public SqlDebugger(int debugContext)
            : this()
        {
            DebuggerContext = debugContext;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        public void SetDebugContext(long context)
        {
            DebuggerContext = context;
        }

        // DEBUG METHODS \\
        public void ServerWrite(string logMsg)
        {
            if (SqlSecurity.ContainsIllegals(logMsg))
            {
                logMsg = SqlSecurity.RemoveIllegals(logMsg);
            }

            string[] batchLog = BreakIntoBatch(logMsg);

            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    connection.Open();
                    foreach (var logItem in batchLog)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append("INSERT INTO cutfloDebug (DebugID, ConsoleWrite)");
                        sb.AppendFormat("VALUES ('{0}', '{1}')", DebuggerContext, logMsg);
                        String sql = sb.ToString();

                        SqlCommand writeCmd = new SqlCommand(sql, connection);

                        writeCmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine("We're fucked the debuggers not even working." + "  :  " + e);
            }
        }

        private string[] BreakIntoBatch(string longStr)
        {
            if (longStr.Length <= 255)
            {
                string[] shortArr = { longStr };
                return shortArr;
            }
            else
            {
                double arrSizeNeeded = longStr.Length / 255;
                arrSizeNeeded = Math.Ceiling(arrSizeNeeded);

                var returnBatch = new string[(int)arrSizeNeeded];

                int lineCtr = 0;

                int initChOfLine = 0;
                int chCtr = 0;

                foreach (var _char in longStr)
                {
                    // Initially 0 and can only have length of 255, thus 254
                    if (chCtr > (lineCtr + 1) * 254)
                    {
                        // found a new line to write
                        string lineStr = longStr.Substring(initChOfLine, 255);
                        initChOfLine = chCtr;
                        returnBatch.SetValue(lineStr, lineCtr);
                        lineCtr++;
                    }
                    // Incrememnt ChCounter every ch read
                    chCtr++;
                }

                return returnBatch;
            }
        }
    }
}