using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Meta
{
    public static class StringExtensions
    {
        /// <summary>
        /// Determines whether the string has white space.
        /// </summary>
        /// <param name="s">The string to test for white space.</param>
        /// <returns>
        ///   <c>true</c> if the string has white space; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasWhiteSpace(this string s)
        {
            if (s == null)
                throw new ArgumentNullException("s");

            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsWhiteSpace(s[i]))
                    return true;
            }
            return false;
        }

        public static string Replace(this string originalString, string oldValue, string newValue, StringComparison comparisonType)
        {
            int startIndex = 0;
            while (true)
            {
                startIndex = originalString.IndexOf(oldValue, startIndex, comparisonType);
                if (startIndex == -1)
                    break;

                originalString = originalString.Substring(0, startIndex) + newValue + originalString.Substring(startIndex + oldValue.Length);

                startIndex += newValue.Length;
            }

            return originalString;
        }
        
        public static string Quote(this string s)
        {
            Regex doubleQuotedString = new Regex("\"[^\\\"]*(\\.[^\\\"]*)*\"");
            if( !doubleQuotedString.IsMatch(s) )
            {
                string quote = "\"";
                return quote + s + quote;
            }

            return s;
        }

        public static string Unquote(this string s)
        {
            if (s[0] == '"' && s[s.Length - 1] == '"')
                return s.Substring(1, s.Length - 1);
            return s;
        }

        public static string NormalizePathSeparators(this string path)
        {
            path = path.Replace("/", @"\");

            Regex doubleSeparator = new Regex(@"\+");
            path = doubleSeparator.Replace(path,@"\");

            if( path[path.Length-1] == '\\' )
                path = path.Substring(0,path.Length-1);

            return path;
        }

    }
}