using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using MySql.Data.MySqlClient;

namespace NinjaTrader.Data
{
    public partial class GTxtParser
    {
        public GTxtParser(string iniFile)
        {
            _months = new List<string>();
            _months.Add("NON");
            _months.Add("JAN");
            _months.Add("FEB");
            _months.Add("MAR");
            _months.Add("APR");
            _months.Add("MAY");
            _months.Add("JUN");
            _months.Add("JUL");
            _months.Add("AUG");
            _months.Add("SEP");
            _months.Add("OCT");
            _months.Add("NOV");
            _months.Add("DEC");

            _loadDate = DateTime.Today.ToString();
            _iniParser = new GIniParser(iniFile);
            _pathTxtData = _iniParser.GetSetting("es", "txtdata");
            _database = _iniParser.GetSetting("options", "database");
            _username = _iniParser.GetSetting("options", "username");
            _password = _iniParser.GetSetting("options", "password");

            _pathCall = _iniParser.GetSetting("options", "call");
            _pathPut = _iniParser.GetSetting("options", "put");
            _pathIndex = _iniParser.GetSetting("options", "index");

            _diCall = new Dictionary<string, string[]>();
            _diPut = new Dictionary<string, string[]>();
            _diIndex = new Dictionary<string, string[]>();

            Dates = GetTradeDates();

            try
            {
                List<string> adirs = new List<string>();

                _dirs = new DirectoryInfo(@_pathTxtData).GetDirectories("????-??-??");

                if (Dates.Count > 0)
                {
                    foreach (DirectoryInfo diNext in _dirs)
                    {
                        adirs.Add(diNext.ToString());
                    }
                }
                else
                {
                    foreach (DirectoryInfo diNext in _dirs)
                    {
                        adirs.Add(diNext.ToString());
                    }
                }

                foreach (string s in adirs)
                {
                    if (File.Exists(@_pathTxtData + s + @"\" + _pathCall + ".txt"))
                        _diCall.Add(s, ReadAllText(@_pathTxtData + s + @"\" + _pathCall + ".txt").Split('\n'));

                    if (File.Exists(@_pathTxtData + s + @"\" + _pathPut + ".txt"))
                        _diPut.Add(s, ReadAllText(@_pathTxtData + s + @"\" + _pathPut + ".txt").Split('\n'));

                    if (File.Exists(@_pathTxtData + s + @"\" + _pathIndex + ".txt"))
                        _diIndex.Add(s, ReadAllText(@_pathTxtData + s + @"\" + _pathIndex + ".txt").Split('\n'));

                }
            }
            catch (Exception e)
            {
                GLog.Print("The process failed: " + e.ToString());
            }

            Levels_Call = new List<LEVELS_CALL_PUT>();
            Levels_Put = new List<LEVELS_CALL_PUT>();
            Levels_Index = new List<LEVELS_INDEX>();
        }

        public string ReadAllText(string path)
        {
            string line;
            StringBuilder sb = new StringBuilder();

            if (File.Exists(path))
            {
                StreamReader file = null;

                try
                {
                    file = new StreamReader(path);

                    while ((line = file.ReadLine()) != null)
                    {
                        sb.AppendLine(line);
                    }
                }
                finally
                {
                    if (file != null)
                        file.Close();
                }
            }
            else
            {
                GLog.Print("File not exist: " + path);
            }

            return sb.ToString();
        }

        public void Parser()
        {
            foreach (KeyValuePair<string, string[]> c in _diCall)
            {
                parserCall(c.Key, c.Value);
            }

            foreach (KeyValuePair<string, string[]> p in _diPut)
            {
                parserPut(p.Key, p.Value);
            }

            foreach (KeyValuePair<string, string[]> p in _diIndex)
            {
                parserIndex(p.Key, p.Value);
            }
        }

        public Dictionary<int, DateTime> GetTradeDates()
        {
            Dictionary<int, DateTime> days = new Dictionary<int, DateTime>();

            if (!isSqlConnetion()) return days;

            try
            {
                String cmdText = "SELECT * FROM es_files";
                MySqlCommand cmd = new MySqlCommand(cmdText, _myConn);

                cmd.Prepare();

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        days[reader.GetDateTime(2).Day] = reader.GetDateTime(2);
                    }
                }

                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                GLog.Print("Error GetTradeDates: " + e.ToString());
            }

            _myConn.Close();
            return days;
        }

        #region MySQL
        private bool isSqlConnetion()
        {
            try
            {
                string constr = @"server=127.0.0.1;database=" + _database + ";userid=" + _username + ";password=" + _password + ";";

                _myConn = new MySqlConnection(constr);
                _myConn.Open();
                return true;
            }
            catch (MySqlException e)
            {
                Console.WriteLine("Error: " + e.ToString());
            }

            return false;
        }

        private MySqlConnection GetSqlConnetion()
        {
            try
            {
                string constr = @"server=127.0.0.1;database=" + _database + ";userid=" + _username + ";password=" + _password + ";";

                MySqlConnection _conn = new MySqlConnection(constr);
                _conn.Open();
                return _conn;
            }
            catch (MySqlException e)
            {
                Console.WriteLine("Error: " + e.ToString());
            }

            return null;
        }

        private void SqlClose()
        {
            _myConn.Close();
        }
        #endregion

        private bool isCallOptions(string line)
        {
            if (line.Trim() == "E-MINI S&P 500 CALL OPTIONS") return true;

            return false;
        }

        private bool isPutOptions(string line)
        {
            if (line.Trim() == "E-MINI S&P 500 PUT OPTIONS") return true;

            return false;
        }

        private bool isFutures(string line)
        {
            if (line.Trim() == "E-MINI S&P FUTURES") return true;

            return false;
        }

        private bool isFut(string line)
        {
            if (line.Trim() == "EMINI S&P FUT") return true;

            return false;
        }

        private bool isFutC(string line)
        {
            if (line.Contains("EMINI S&P FUT")) return true;

            return false;
        }

        private bool isPreliminary(string line)
        {
            if (line.Trim() == "PRELIMINARY") return true;

            return false;
        }

        private bool isFinal(string line)
        {
            if (line.Trim() == "FINAL") return true;

            return false;
        }

        private bool isCall(string month, string line)
        {
            string str = "^" + month + @"\sMINI S&P C";
            Regex regex = new Regex(str);

            string str1 = "^" + month + @"\sEMINI S&P C";
            Regex regex1 = new Regex(str1);

            if (regex.IsMatch(line) || regex1.IsMatch(line)) return true;

            return false;
        }

        private bool isPut(string month, string line)
        {
            string str = "^" + month + @"\sMINI S&P P";
            Regex regex = new Regex(str);

            string str1 = "^" + month + @"\sEMINI S&P P";
            Regex regex1 = new Regex(str1);

            if (regex.IsMatch(line) || regex1.IsMatch(line)) return true;

            return false;
        }

        private bool isTotal(string line)
        {
            if (line.Contains("TOTAL")) return true;

            return false;
        }

        private bool isDaily(string line)
        {
            if (line.Contains("DAILY INFORMATION BULLETIN")) return true;

            return false;
        }
    }
}
