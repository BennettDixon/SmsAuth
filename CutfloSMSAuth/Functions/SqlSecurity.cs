using System;
namespace CutfloSMSAuth
{
    public class SqlSecurity
    {
        private static readonly string[] IllegalsAsStrings = { "\'", "\"", ";" };
        private static readonly char[] IllegalsAsChars = { '\'', '\"', ';' };

        public static bool ContainsIllegals(string checkValue)
        {
            foreach (var illegal in IllegalsAsStrings)
            {
                if (checkValue.Contains(illegal))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool BatchContainsIllegals(string[] checkValues)
        {
            foreach (var checkVal in checkValues)
            {
                if (checkVal == null) continue;

                foreach (var illegal in IllegalsAsStrings)
                {
                    if (checkVal.Contains(illegal))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static string RemoveIllegals(string checkValue)
        {
            string nonIllegalStr = string.Empty;

            var testArray = checkValue.ToCharArray();

            foreach (var _char in testArray)
            {
                if (_char == IllegalsAsChars[0])
                {
                    continue;
                }
                else if (_char == IllegalsAsChars[1])
                {
                    continue;
                }
                else if (_char == IllegalsAsChars[2])
                {
                    continue;
                }
                else
                {
                    nonIllegalStr += _char;
                }
            }

            return nonIllegalStr;
        }
    }
}