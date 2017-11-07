using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;

//using iTextSharp.text.pdf;
//using iTextSharp.text.pdf.parser;

using MySql.Data.MySqlClient;

using NinjaTrader.Data;

namespace NinjaTrader.Data
{
    public class GPdfParserOptions
    {
        private GIniParser _iniParser;
        private string _loadDate;
        private string _tradeDate;
        private string _pathCall;
        private string _pathPut;
        private string _pathIndex;

        private string _pathTxtData;
        private DirectoryInfo[] _dirs;

        private string _database;
        private string _username;
        private string _password;

        private Dictionary<string, string[]> _diCall;
        private Dictionary<string, string[]> _diPut;
        private Dictionary<string, string[]> _diIndex;
        private List<string> _months;

        private MySqlConnection _myConn;

        public IList<string> Body;

        public struct LEVELS_CALL_PUT
        {
            public int strike;
            public DateTime tradedate;
            public int oi;
            public int oi_ch;
            public double sett_price;
            public int sett_price_ch;
            public int volume;
            public double level;
            public double level_high;
            public double level_low;
            public double open_range;

            public LEVELS_CALL_PUT(int strike, DateTime tradedate, int oi, int oi_ch, double sett_price, int sett_price_ch, int volume, double level, double level_high, double level_low, double open_range)
            {
                this.strike = strike;
                this.tradedate = tradedate;
                this.oi = oi;
                this.oi_ch = oi_ch;
                this.sett_price = sett_price;
                this.sett_price_ch = sett_price_ch;
                this.volume = volume;
                this.level = Math.Round(level, 2);
                this.level_high = Math.Round(level_high, 2);
                this.level_low = Math.Round(level_low, 2);
                this.open_range = open_range;
            }
        }

        public List<LEVELS_CALL_PUT> Levels_Call;
        public List<LEVELS_CALL_PUT> Levels_Put;
        public Dictionary<int, DateTime> Dates;

        public GPdfParserOptions(string iniFile)
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

                        /*foreach (KeyValuePair<int, DateTime> k in Dates)
                        {
                            if (diNext.ToString() == k.Value.ToString("yyyy-MM-dd"))
                            {
                                adirs.Remove(diNext.ToString());
                            }
                        }*/
                    }
                }
                else
                {
                    foreach (DirectoryInfo diNext in _dirs)
                    {
                        adirs.Add(diNext.ToString());
                    }
                }

                foreach(string s in adirs)
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
                Console.WriteLine("File not exist {0}", path);
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

        private void parserCall(string _tradeDate, string[] _strCall)
        {
            if (!isSqlConnetion()) return;

            GLog.Print("PARSER CALL: start " + _tradeDate);

            #region DELETE ES_FILES, ES_CALL, ES_FUT, ES_EO
            try
            {
                MySqlCommand cmd = new MySqlCommand("DELETE FROM es_files WHERE tradedate=@tradedate and name=@name", _myConn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@name", "CALL");
                cmd.Parameters.AddWithValue("@tradedate", _tradeDate);
                cmd.ExecuteNonQuery();
                GLog.Print("--PARSER: DELETE ES_FILES " + _tradeDate);
            }
            catch (MySqlException e)
            {
                GLog.Print("--Error DELETE ES_FILES: " + e.ToString());
            }

            try
            {
                MySqlCommand cmd = new MySqlCommand("DELETE FROM es_call WHERE tradedate=@tradedate", _myConn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", _tradeDate);
                cmd.ExecuteNonQuery();
                GLog.Print("--PARSER: DELETE ES_CALL " + _tradeDate);
            }
            catch (MySqlException e)
            {
                GLog.Print("--Error DELETE ES_CALL: " + e.ToString());
            }

            try
            {
                MySqlCommand cmd = new MySqlCommand("DELETE FROM es_fut WHERE tradedate=@tradedate", _myConn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", _tradeDate);
                cmd.ExecuteNonQuery();
                GLog.Print("--PARSER: DELETE ES_FUT " + _tradeDate);
            }
            catch (MySqlException e)
            {
                GLog.Print("--Error DELETE ES_FUT: " + e.ToString());
            }

            try
            {
                MySqlCommand cmd = new MySqlCommand("DELETE FROM es_eo WHERE tradedate=@tradedate", _myConn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", _tradeDate);
                cmd.ExecuteNonQuery();
                GLog.Print("--PARSER: DELETE ES_EO " + _tradeDate);
            }
            catch (MySqlException e)
            {
                GLog.Print("--Error DELETE ES_EO: " + e.ToString());
            }
            #endregion

            #region INSERT ES_FILES
            try
            {
                String cmdText = "INSERT INTO es_files (name, tradedate, date) VALUES (@name, @tradedate, @date)";
                MySqlCommand cmd = new MySqlCommand(cmdText, _myConn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@name", "CALL");
                cmd.Parameters.AddWithValue("@tradedate", _tradeDate);
                cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd"));
                cmd.ExecuteNonQuery();
                GLog.Print("--PARSER: INSERT ES_FILES " + _tradeDate);
            }
            catch (MySqlException e)
            {
                GLog.Print("--Error INSERT ES_FILES: " + e.ToString());
            }
            #endregion

            bool fstart = false;
            string ss = "";
            string smonth = "";
            string sname = "";
            string sm_d = "";

            for (int i = 0; i < _strCall.Length; i++)
            {
                if (isCallOptions(_strCall[i]))
                {
                    fstart = true;
                }
                else
                {
                    if (fstart)
                    {
                        if ((isPreliminary(_strCall[i]) && isFutC(_strCall[i + 2])) || (isFinal(_strCall[i]) && isFutC(_strCall[i + 2])))
                        {
                            i += 2;

                            string tstr = _strCall[i - 1].Split(' ')[0];

                            i++;
                            string[] teom = _strCall[i].Trim().Split(' ');

                            i++;
                            string[] teow1 = _strCall[i].Trim().Split(' ');

                            i++;
                            string[] teow2 = _strCall[i].Trim().Split(' ');

                            i++;
                            string[] teow4 = _strCall[i].Trim().Split(' ');

                            #region INSERT ES_EO
                            try
                            {
                                for (int p = 4; p < teom.Length; p++)
                                {
                                    String cmdText = "INSERT INTO es_eo (tradedate, month, name, m_d, m, d) VALUES (@tradedate, @month, @name, @m_d, @m, @d)";
                                    MySqlCommand cmd = new MySqlCommand(cmdText, _myConn);

                                    cmd.Prepare();
                                    cmd.Parameters.AddWithValue("@tradedate", _tradeDate);
                                    cmd.Parameters.AddWithValue("@name", "EOM");
                                    cmd.Parameters.AddWithValue("@month", tstr);
                                    cmd.Parameters.AddWithValue("@m_d", teom[p]);
                                    cmd.Parameters.AddWithValue("@m", Convert.ToUInt16(teom[p].Split('/')[0]));
                                    cmd.Parameters.AddWithValue("@d", Convert.ToUInt16(teom[p].Split('/')[1]));
                                    cmd.ExecuteNonQuery();
                                }

                                for (int p = 4; p < teow1.Length; p++)
                                {
                                    String cmdText = "INSERT INTO es_eo (tradedate, month, name, m_d, m, d) VALUES (@tradedate, @month, @name, @m_d, @m, @d)";
                                    MySqlCommand cmd = new MySqlCommand(cmdText, _myConn);

                                    cmd.Prepare();
                                    cmd.Parameters.AddWithValue("@tradedate", _tradeDate);
                                    cmd.Parameters.AddWithValue("@name", "EOW1");
                                    cmd.Parameters.AddWithValue("@month", tstr);
                                    cmd.Parameters.AddWithValue("@m_d", teow1[p]);
                                    cmd.Parameters.AddWithValue("@m", Convert.ToUInt16(teow1[p].Split('/')[0]));
                                    cmd.Parameters.AddWithValue("@d", Convert.ToUInt16(teow1[p].Split('/')[1]));
                                    cmd.ExecuteNonQuery();
                                }

                                for (int p = 4; p < teow2.Length; p++)
                                {
                                    String cmdText = "INSERT INTO es_eo (tradedate, month, name, m_d, m, d) VALUES (@tradedate, @month, @name, @m_d, @m, @d)";
                                    MySqlCommand cmd = new MySqlCommand(cmdText, _myConn);

                                    cmd.Prepare();
                                    cmd.Parameters.AddWithValue("@tradedate", _tradeDate);
                                    cmd.Parameters.AddWithValue("@name", "EOW2");
                                    cmd.Parameters.AddWithValue("@month", tstr);
                                    cmd.Parameters.AddWithValue("@m_d", teow2[p]);
                                    cmd.Parameters.AddWithValue("@m", Convert.ToUInt16(teow2[p].Split('/')[0]));
                                    cmd.Parameters.AddWithValue("@d", Convert.ToUInt16(teow2[p].Split('/')[1]));
                                    cmd.ExecuteNonQuery();
                                }

                                for (int p = 4; p < teow4.Length; p++)
                                {
                                    String cmdText = "INSERT INTO es_eo (tradedate, month, name, m_d, m, d) VALUES (@tradedate, @month, @name, @m_d, @m, @d)";
                                    MySqlCommand cmd = new MySqlCommand(cmdText, _myConn);

                                    cmd.Prepare();
                                    cmd.Parameters.AddWithValue("@tradedate", _tradeDate);
                                    cmd.Parameters.AddWithValue("@name", "EOW4");
                                    cmd.Parameters.AddWithValue("@month", tstr);
                                    cmd.Parameters.AddWithValue("@m_d", teow4[p]);
                                    cmd.Parameters.AddWithValue("@m", Convert.ToUInt16(teow4[p].Split('/')[0]));
                                    cmd.Parameters.AddWithValue("@d", Convert.ToUInt16(teow4[p].Split('/')[1]));
                                    cmd.ExecuteNonQuery();
                                }

                                GLog.Print("--PARSER: INSERT ES_EO " + _tradeDate);
                            }
                            catch (MySqlException e)
                            {
                                GLog.Print("Error INSERT ES_EO: " + e.ToString());
                            }
                            #endregion

                            #region SELECT ES_EO
                            try
                            {
                                DateTime dt = Convert.ToDateTime(_tradeDate);
                                String cmdText = "SELECT * FROM es_eo WHERE tradedate=@tradedate and m=@m and d>=@d ORDER BY m, d ASC LIMIT 1";
                                MySqlCommand cmd = new MySqlCommand(cmdText, _myConn);

                                cmd.Prepare();
                                cmd.Parameters.AddWithValue("@tradedate", _tradeDate);
                                cmd.Parameters.AddWithValue("@m", dt.Month);
                                cmd.Parameters.AddWithValue("@d", dt.Day);

                                using (MySqlDataReader reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        smonth = reader.GetString(2);
                                        sname = reader.GetString(3);
                                        sm_d = reader.GetString(4);
                                        ss = smonth + " " + sname;
                                    }
                                }

                                cmd.ExecuteNonQuery();
                                GLog.Print("--PARSER: SELECT ES_EO " + _tradeDate);
                            }
                            catch (MySqlException e)
                            {
                                GLog.Print("Error SELECT ES_EO: " + e.ToString());
                            }
                            #endregion
                        }

                        if (isFutures(_strCall[i]) && isFut(_strCall[i + 11]))
                        {
                            i += 11;

                            for (int k = 0; k < 4; k++)
                            {
                                i++;

                                #region INSERT ES_FUT
                                string[] tstr = _strCall[i].Split(' ');

                                try
                                {
                                    String cmdText = "INSERT INTO es_fut (month, tradedate, open_range, high, low, sett_price, gl_volume, open_interest, oi_modif) VALUES (@month, @tradedate, @open_range, @high, @low, @sett_price, @gl_volume, @open_interest, @oi_modif)";
                                    MySqlCommand cmd = new MySqlCommand(cmdText, _myConn);

                                    cmd.Prepare();
                                    cmd.Parameters.AddWithValue("@month", tstr[0]);
                                    cmd.Parameters.AddWithValue("@tradedate", _tradeDate);
                                    cmd.Parameters.AddWithValue("@open_range", tstr[1]);
                                    cmd.Parameters.AddWithValue("@high", tstr[2]);
                                    cmd.Parameters.AddWithValue("@low", tstr[3]);
                                    cmd.Parameters.AddWithValue("@sett_price", tstr[5] + ";" + tstr[6] + ";" + tstr[7]);
                                    cmd.Parameters.AddWithValue("@gl_volume", tstr[9]);
                                    cmd.Parameters.AddWithValue("@open_interest", tstr[10]);

                                    string s = tstr[12];
                                    string t = "";

                                    if (s.Contains("."))
                                    {
                                        if (s.Contains("A") || s.Contains("B"))
                                        {
                                            t = s.Remove(s.Length - 8);
                                        }
                                        else
                                        {
                                            t = s.Remove(s.Length - 7);
                                        }
                                    }

                                    cmd.Parameters.AddWithValue("@oi_modif", tstr[11] + ((t != "") ? t : s));
                                    cmd.ExecuteNonQuery();
                                }
                                catch (MySqlException e)
                                {
                                    GLog.Print("Error INSERT ES_FUT: " + e.ToString());
                                }
                                #endregion
                            }

                            GLog.Print("--PARSER: INSERT ES_FUT " + _tradeDate);
                        }

                        if (isCall(ss, _strCall[i]))
                        {
                            GLog.Print("--CALL parsing str: " + ss);

                            while (!isTotal(_strCall[i]))
                            {
                                Regex regex = new Regex(@"^[0-9]{4}\s");

                                if (regex.IsMatch(_strCall[i]) && !isDaily(_strCall[i]))
                                {
                                    #region INSERT ES_CALL
                                    string[] tstr = _strCall[i].Split(' ');

                                    try
                                    {
                                        String cmdText = "INSERT INTO es_call (strike, tradedate, month, open_range, high, low, sett_price, sett_price_ch, delta, volume, open_interest, open_interest_ch, high_contract, low_contract, oi_sp, eo_name, eo_m_d) VALUES (@strike, @tradedate, @month, @open_range, @high, @low, @sett_price, @sett_price_ch, @delta, @volume, @open_interest, @open_interest_ch, @high_contract, @low_contract, @oi_sp, @eo_name, @eo_m_d)";
                                        MySqlCommand cmd = new MySqlCommand(cmdText, _myConn);

                                        cmd.Prepare();
                                        cmd.Parameters.AddWithValue("@eo_name", sname);
                                        cmd.Parameters.AddWithValue("@eo_m_d", sm_d);
                                        cmd.Parameters.AddWithValue("@strike", tstr[0]);
                                        cmd.Parameters.AddWithValue("@tradedate", _tradeDate);
                                        cmd.Parameters.AddWithValue("@month", smonth.Substring(0, 3));
                                        cmd.Parameters.AddWithValue("@open_range", tstr[1]);
                                        cmd.Parameters.AddWithValue("@high", tstr[2]);
                                        cmd.Parameters.AddWithValue("@low", tstr[3]);
                                        cmd.Parameters.AddWithValue("@sett_price", tstr[5]);

                                        if (tstr[5] != "CAB")
                                        {
                                            cmd.Parameters.AddWithValue("@sett_price_ch", (tstr[6] != "UNCH" ? tstr[5][tstr[5].Length - 1] + tstr[6] : ""));

                                            string st = tstr[5];
                                            int last = st.Length - 1;

                                            cmd.Parameters.AddWithValue("@oi_sp", float.Parse((st[last] == '-' || st[last] == '+') ? st.Remove(last) : st, CultureInfo.InvariantCulture) * (tstr[10] == "----" ? 0 : int.Parse(tstr[10])));
                                        }
                                        else
                                        {
                                            cmd.Parameters.AddWithValue("@sett_price_ch", 0);
                                            cmd.Parameters.AddWithValue("@oi_sp", 0);
                                        }

                                        cmd.Parameters.AddWithValue("@delta", tstr[7]);
                                        cmd.Parameters.AddWithValue("@volume", tstr[9]);
                                        cmd.Parameters.AddWithValue("@open_interest", tstr[10]);


                                        if (tstr[11] == "UNCH")
                                        {
                                            cmd.Parameters.AddWithValue("@open_interest_ch", 0);
                                            cmd.Parameters.AddWithValue("@high_contract", tstr[12]);
                                            cmd.Parameters.AddWithValue("@low_contract", tstr[13]);
                                        }
                                        else
                                        {
                                            cmd.Parameters.AddWithValue("@open_interest_ch", tstr[11] + tstr[12]);
                                            cmd.Parameters.AddWithValue("@high_contract", tstr[13]);
                                            cmd.Parameters.AddWithValue("@low_contract", tstr[14]);
                                        }

                                        cmd.ExecuteNonQuery();
                                        //GLog.Print("--PARSER: INSERT ES_CALL " + _tradeDate);
                                    }
                                    catch (MySqlException e)
                                    {
                                        GLog.Print("Error INSERT ES_CALL: " + e.ToString());
                                    }
                                    #endregion
                                }

                                i++;
                            }
                        }
                    }
                }
            }

            GLog.Print("PARSER CALL: stop " + _tradeDate);
            SqlClose();
        }

        private void parserPut(string _tradeDate, string[] _strPut)
        {
            if (!isSqlConnetion()) return;

            GLog.Print("PARSER PUT: start " + _tradeDate);

            #region DELETE ES_FILES, ES_PUT
            try
            {
                MySqlCommand cmd = new MySqlCommand("DELETE FROM es_files WHERE tradedate=@tradedate and name=@name", _myConn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@name", "PUT");
                cmd.Parameters.AddWithValue("@tradedate", _tradeDate);
                cmd.ExecuteNonQuery();
                GLog.Print("--PARSER: DELETE ES_FILES " + _tradeDate);
            }
            catch (MySqlException e)
            {
                GLog.Print("--Error DELETE ES_FILES: " + e.ToString());
            }

            try
            {
                MySqlCommand cmd = new MySqlCommand("DELETE FROM es_put WHERE tradedate=@tradedate", _myConn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", _tradeDate);
                cmd.ExecuteNonQuery();
                GLog.Print("--PARSER: DELETE ES_PUT " + _tradeDate);
            }
            catch (MySqlException e)
            {
                GLog.Print("--Error DELETE ES_PUT: " + e.ToString());
            }
            #endregion

            #region INSERT ES_FILES
            try
            {
                String cmdText = "INSERT INTO es_files (name, tradedate, date) VALUES (@name, @tradedate, @date)";
                MySqlCommand cmd = new MySqlCommand(cmdText, _myConn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@name", "PUT");
                cmd.Parameters.AddWithValue("@tradedate", _tradeDate);
                cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd"));
                cmd.ExecuteNonQuery();
                GLog.Print("--PARSER: INSERT ES_FILES " + _tradeDate);
            }
            catch (MySqlException e)
            {
                GLog.Print("--Error INSERT ES_FILES: " + e.ToString());
            }
            #endregion

            string ss = "";
            string smonth = "";
            string sname = "";
            string sm_d = "";

            #region SELECT ES_EO
            try
            {
                DateTime dt = Convert.ToDateTime(_tradeDate);

                String cmdText = "SELECT * FROM es_eo WHERE tradedate=@tradedate and m=@m and d>=@d ORDER BY m, d ASC LIMIT 1";
                MySqlCommand cmd = new MySqlCommand(cmdText, _myConn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", _tradeDate);
                cmd.Parameters.AddWithValue("@m", dt.Month);
                cmd.Parameters.AddWithValue("@d", dt.Day);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        smonth = reader.GetString(2);
                        sname = reader.GetString(3);
                        sm_d = reader.GetString(4);
                        ss = smonth + " " + sname;
                    }
                }

                cmd.ExecuteNonQuery();
                GLog.Print("--PARSER: SELECT ES_EO " + _tradeDate);
            }
            catch (MySqlException e)
            {
                GLog.Print("Error SELECT ES_EO: " + e.ToString());
            }
            #endregion

            for (int i = 0; i < _strPut.Length; i++)
            {
                if (isPut(ss, _strPut[i]))
                {
                    while (!isTotal(_strPut[i]))
                    {
                        Regex regex = new Regex(@"^[0-9]{4}\s");

                        if (regex.IsMatch(_strPut[i]) && !isDaily(_strPut[i]))
                        {
                            #region INSERT ES_PUT
                            string[] tstr = _strPut[i].Split(' ');

                            try
                            {
                                String cmdText = "INSERT INTO es_put (strike, tradedate, month, open_range, high, low, sett_price, sett_price_ch, delta, volume, open_interest, open_interest_ch, high_contract, low_contract, oi_sp, eo_name, eo_m_d) VALUES (@strike, @tradedate, @month, @open_range, @high, @low, @sett_price, @sett_price_ch, @delta, @volume, @open_interest, @open_interest_ch, @high_contract, @low_contract, @oi_sp, @eo_name, @eo_m_d)";
                                MySqlCommand cmd = new MySqlCommand(cmdText, _myConn);

                                cmd.Prepare();
                                cmd.Parameters.AddWithValue("@eo_name", sname);
                                cmd.Parameters.AddWithValue("@eo_m_d", sm_d);
                                cmd.Parameters.AddWithValue("@strike", tstr[0]);
                                cmd.Parameters.AddWithValue("@tradedate", _tradeDate);
                                cmd.Parameters.AddWithValue("@month", smonth.Substring(0, 3));
                                cmd.Parameters.AddWithValue("@open_range", tstr[1]);
                                cmd.Parameters.AddWithValue("@high", tstr[2]);
                                cmd.Parameters.AddWithValue("@low", tstr[3]);
                                cmd.Parameters.AddWithValue("@sett_price", tstr[5]);

                                if (tstr[5] != "CAB")
                                {
                                    cmd.Parameters.AddWithValue("@sett_price_ch", (tstr[6] != "UNCH" ? tstr[5][tstr[5].Length - 1] + tstr[6] : ""));

                                    string st = tstr[5];
                                    int last = st.Length - 1;

                                    cmd.Parameters.AddWithValue("@oi_sp", float.Parse((st[last] == '-' || st[last] == '+') ? st.Remove(last) : st, CultureInfo.InvariantCulture) * (tstr[10] == "----" ? 0 : int.Parse(tstr[10])));
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue("@sett_price_ch", 0);
                                    cmd.Parameters.AddWithValue("@oi_sp", 0);
                                }

                                cmd.Parameters.AddWithValue("@delta", tstr[7]);
                                cmd.Parameters.AddWithValue("@volume", tstr[9]);

                                if (tstr[11] == "UNCH")
                                {
                                    cmd.Parameters.AddWithValue("@open_interest", tstr[10]);
                                    cmd.Parameters.AddWithValue("@open_interest_ch", 0);
                                    cmd.Parameters.AddWithValue("@high_contract", tstr[12]);
                                    cmd.Parameters.AddWithValue("@low_contract", tstr[13]);
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue("@open_interest", tstr[10]);
                                    cmd.Parameters.AddWithValue("@open_interest_ch", tstr[11] + tstr[12]);
                                    cmd.Parameters.AddWithValue("@high_contract", tstr[13]);
                                    cmd.Parameters.AddWithValue("@low_contract", tstr[14]);
                                }

                                cmd.ExecuteNonQuery();
                                //GLog.Print("--PARSER: INSERT ES_PUT " + _tradeDate);
                            }
                            catch (MySqlException e)
                            {
                                GLog.Print("Error INSERT ES_PUT: " + e.ToString());
                            }
                            #endregion
                        }

                        i++;
                    }
                }
            }

            GLog.Print("PARSER PUT: stop " + _tradeDate);
            SqlClose();
        }

        private void parserIndex(string _tradeDate, string[] _strIndex)
        {
            if (!isSqlConnetion()) return;

            GLog.Print("PARSER INDEX: start " + _tradeDate);

            #region DELETE ES_INDEX
            try
            {
                MySqlCommand cmd = new MySqlCommand("DELETE FROM es_index WHERE tradedate=@tradedate", _myConn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", _tradeDate);
                cmd.ExecuteNonQuery();
                GLog.Print("--PARSER: DELETE ES_INDEX " + _tradeDate);
            }
            catch (MySqlException e)
            {
                GLog.Print("--Error DELETE ES_INDEX: " + e.ToString());
            }
            #endregion

            for (int i = 0; i < _strIndex.Length; i++)
            {
                if (isFut(_strIndex[i]))
                {
                    for (int k = 0; k < 4; k++)
                    {
                        i++;

                        #region INSERT ES_INDEX
                        string[] tstr = _strIndex[i].Split(' ');

                        try
                        {
                            String cmdText = "INSERT INTO es_index (month, tradedate, volume, open_interest, open_interest_ch, high_contract, low_contract, pit_high, pit_high_a_b, pit_low, pit_low_a_b, pit_open_range) VALUES (@month, @tradedate, @volume, @open_interest, @open_interest_ch, @high_contract, @low_contract, @pit_high, @pit_high_a_b, @pit_low, @pit_low_a_b, @pit_open_range)";
                            MySqlCommand cmd = new MySqlCommand(cmdText, _myConn);

                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@month", tstr[0]);
                            cmd.Parameters.AddWithValue("@tradedate", _tradeDate);
                            cmd.Parameters.AddWithValue("@pit_open_range", tstr[1]);
                            cmd.Parameters.AddWithValue("@pit_high", tstr[2]);
                            cmd.Parameters.AddWithValue("@pit_low", tstr[3]);
                            //cmd.Parameters.AddWithValue("@sett_price", tstr[5] + ";" + tstr[6] + ";" + tstr[7]);
                            cmd.Parameters.AddWithValue("@volume", tstr[9]);
                            cmd.Parameters.AddWithValue("@open_interest", tstr[10]);
                            cmd.Parameters.AddWithValue("@open_interest_ch", tstr[11] + tstr[12]);
                            cmd.Parameters.AddWithValue("@high_contract", tstr[13]);
                            cmd.Parameters.AddWithValue("@low_contract", tstr[14]);

                            cmd.ExecuteNonQuery();
                        }
                        catch (MySqlException e)
                        {
                            GLog.Print("Error INSERT ES_INDEX: " + e.ToString());
                        }
                        #endregion
                    }

                    GLog.Print("--PARSER: INSERT ES_INDEX " + _tradeDate);
                    break;
                }
            }

            GLog.Print("PARSER INDEX: stop " + _tradeDate);
            SqlClose();

        }

        public void BuildCallLevels(int cntLevel, string tradeDate, int month, int oi, int vol, double delta)
        {
            MySqlConnection conn = GetSqlConnetion();

            #region DELETE ES_CALL_LEVELS
            try
            {
                MySqlCommand cmd = new MySqlCommand("DELETE FROM es_call_levels WHERE tradedate=@tradedate", conn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", tradeDate);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Console.WriteLine("Error: " + e.ToString());
            }
            #endregion

            #region DELETE ES_CALL_LEVELS_VOL
            try
            {
                MySqlCommand cmd = new MySqlCommand("DELETE FROM es_call_levels_vol WHERE tradedate=@tradedate", conn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", tradeDate);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Console.WriteLine("Error: " + e.ToString());
            }
            #endregion

            #region SELECT ES_CALL_LEVELS
            try
            {
                MySqlCommand cmd = new MySqlCommand("SELECT * FROM es_call WHERE month=@month and tradedate=@tradedate and open_interest >= @oi and delta >= @delta ORDER BY open_interest DESC LIMIT 0,@cnt", conn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", tradeDate);
                cmd.Parameters.AddWithValue("@month", _months[month]);
                cmd.Parameters.AddWithValue("@cnt", cntLevel);
                cmd.Parameters.AddWithValue("@oi", oi);
                cmd.Parameters.AddWithValue("@delta", delta);

                GLog.Print("begin call_level month: " + _months[month]);
                GLog.Print("----tradedate: " + tradeDate);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    MySqlConnection conn1 = GetSqlConnetion();

                    while (reader.Read())
                    {
                        #region INSERT ES_CALL_LEVELS
                        try
                        {
                            GLog.Print("----strike: " + reader.GetString(1));

                            MySqlCommand cmd1 = new MySqlCommand("INSERT INTO es_call_levels (strike, tradedate, oi, oi_ch, sett_price, sett_price_ch, volume, high_contract, low_contract, open_range, level, level_high, level_low, oi_sp, delta, month) VALUES (@strike, @tradedate, @oi, @oi_ch, @sett_price, @sett_price_ch, @volume, @high_contract, @low_contract, @open_range, @level, @level_high, @level_low, @oi_sp, @delta, @month)", conn1);

                            cmd1.Prepare();
                            cmd1.Parameters.AddWithValue("@strike", reader.GetString(1));
                            cmd1.Parameters.AddWithValue("@tradedate", tradeDate);
                            cmd1.Parameters.AddWithValue("@oi", reader.GetString(4));
                            cmd1.Parameters.AddWithValue("@oi_ch", reader.GetString(5));
                            cmd1.Parameters.AddWithValue("@sett_price", reader.GetFloat(13));
                            cmd1.Parameters.AddWithValue("@sett_price_ch", reader.GetString(14));
                            cmd1.Parameters.AddWithValue("@volume", reader.GetString(3));

                            string st = reader.GetString(10);
                            int last = st.Length - 1;

                            if (st[last] == 'B')
                            {
                                cmd1.Parameters.AddWithValue("@high_contract", st.Remove(last));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@high_contract", st);
                            }
         
                            st = reader.GetString(11);
                            last = st.Length - 1;

                            if (st[last] == 'A')
                            {
                                cmd1.Parameters.AddWithValue("@low_contract", st.Remove(last));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@low_contract", st);
                            }
     
                            st = (reader.GetString(6)[0] == '*' || reader.GetString(6)[0] == '#' ? reader.GetString(6).Remove(0, 1) : reader.GetString(6));
                            last = st.Length - 1;
                 
                            if (st[last] == 'B')
                            {
                                cmd1.Parameters.AddWithValue("@level_high", (reader.GetFloat(1) + float.Parse(st.Remove(last), CultureInfo.InvariantCulture) / 10));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@level_high", (reader.GetInt16(1) + float.Parse(st, CultureInfo.InvariantCulture) / 10));
                            }

                            st = (reader.GetString(7)[0] == '*' || reader.GetString(7)[0] == '#' ? reader.GetString(7).Remove(0, 1) : reader.GetString(7));
                            last = st.Length - 1;
             
                            if (st[last] == 'A')
                            {
                                cmd1.Parameters.AddWithValue("@level_low", (reader.GetFloat(1) + float.Parse(st.Remove(last), CultureInfo.InvariantCulture) / 10));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@level_low", (reader.GetInt16(1) + float.Parse(st, CultureInfo.InvariantCulture) / 10));
                            }
                  
                            cmd1.Parameters.AddWithValue("@open_range", reader.GetFloat(12));
                            cmd1.Parameters.AddWithValue("@level", (reader.GetInt16(1) + reader.GetFloat(13) / 10));
                            cmd1.Parameters.AddWithValue("@oi_sp", reader.GetFloat(15));
                            cmd1.Parameters.AddWithValue("@delta", reader.GetFloat(2));
                            cmd1.Parameters.AddWithValue("@month", _months[month]);
                            cmd1.ExecuteNonQuery();
                        }
                        catch (MySqlException e)
                        {
                            GLog.Print("Error INSERT ES_CALL_LEVELS: " + e.ToString());
                        }
                        #endregion
                    }

                    conn1.Close();
                }

                cmd.ExecuteNonQuery();
                GLog.Print("end call_level");
            }
            catch (MySqlException e)
            {
                GLog.Print("Error SELECT ES_CALL_LEVELS: " + e.ToString());
            }
            #endregion

            #region SELECT ES_CALL_LEVELS_VOL
            try
            {
                MySqlCommand cmd = new MySqlCommand("SELECT * FROM es_call WHERE month=@month and tradedate=@tradedate and volume >= @vol and delta >= @delta ORDER BY volume DESC LIMIT 0,@cnt", conn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", tradeDate);
                cmd.Parameters.AddWithValue("@month", _months[month]);
                cmd.Parameters.AddWithValue("@cnt", cntLevel);
                cmd.Parameters.AddWithValue("@vol", vol);
                cmd.Parameters.AddWithValue("@delta", delta);

                GLog.Print("begin call_level_vol month: " + _months[month]);
                GLog.Print("----tradedate: " + tradeDate);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    MySqlConnection conn1 = GetSqlConnetion();

                    while (reader.Read())
                    {
                        #region INSERT ES_CALL_LEVELS
                        try
                        {
                            GLog.Print("----strike: " + reader.GetString(1));

                            MySqlCommand cmd1 = new MySqlCommand("INSERT INTO es_call_levels_vol (strike, tradedate, oi, oi_ch, sett_price, sett_price_ch, volume, high_contract, low_contract, open_range, level, level_high, level_low, oi_sp, delta, month) VALUES (@strike, @tradedate, @oi, @oi_ch, @sett_price, @sett_price_ch, @volume, @high_contract, @low_contract, @open_range, @level, @level_high, @level_low, @oi_sp, @delta, @month)", conn1);

                            cmd1.Prepare();
                            cmd1.Parameters.AddWithValue("@strike", reader.GetString(1));
                            cmd1.Parameters.AddWithValue("@tradedate", tradeDate);
                            cmd1.Parameters.AddWithValue("@oi", reader.GetString(4));
                            cmd1.Parameters.AddWithValue("@oi_ch", reader.GetString(5));
                            cmd1.Parameters.AddWithValue("@sett_price", reader.GetFloat(13));
                            cmd1.Parameters.AddWithValue("@sett_price_ch", reader.GetString(14));
                            cmd1.Parameters.AddWithValue("@volume", reader.GetString(3));

                            string st = reader.GetString(10);
                            int last = st.Length - 1;

                            if (st[last] == 'B')
                            {
                                cmd1.Parameters.AddWithValue("@high_contract", st.Remove(last));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@high_contract", st);
                            }

                            st = reader.GetString(11);
                            last = st.Length - 1;

                            if (st[last] == 'A')
                            {
                                cmd1.Parameters.AddWithValue("@low_contract", st.Remove(last));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@low_contract", st);
                            }

                            st = (reader.GetString(6)[0] == '*' || reader.GetString(6)[0] == '#' ? reader.GetString(6).Remove(0, 1) : reader.GetString(6));
                            last = st.Length - 1;

                            if (st[last] == 'B')
                            {
                                cmd1.Parameters.AddWithValue("@level_high", (reader.GetFloat(1) + float.Parse(st.Remove(last), CultureInfo.InvariantCulture) / 10));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@level_high", (reader.GetInt16(1) + float.Parse(st, CultureInfo.InvariantCulture) / 10));
                            }

                            st = (reader.GetString(7)[0] == '*' || reader.GetString(7)[0] == '#' ? reader.GetString(7).Remove(0, 1) : reader.GetString(7));
                            last = st.Length - 1;

                            if (st[last] == 'A')
                            {
                                cmd1.Parameters.AddWithValue("@level_low", (reader.GetFloat(1) + float.Parse(st.Remove(last), CultureInfo.InvariantCulture) / 10));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@level_low", (reader.GetInt16(1) + float.Parse(st, CultureInfo.InvariantCulture) / 10));
                            }

                            cmd1.Parameters.AddWithValue("@open_range", reader.GetFloat(12));
                            cmd1.Parameters.AddWithValue("@level", (reader.GetInt16(1) + reader.GetFloat(13) / 10));
                            cmd1.Parameters.AddWithValue("@oi_sp", reader.GetFloat(15));
                            cmd1.Parameters.AddWithValue("@delta", reader.GetFloat(2));
                            cmd1.Parameters.AddWithValue("@month", _months[month]);
                            cmd1.ExecuteNonQuery();
                        }
                        catch (MySqlException e)
                        {
                            GLog.Print("Error INSERT ES_CALL_LEVELS_VOL: " + e.ToString());
                        }
                        #endregion
                    }

                    conn1.Close();
                }

                cmd.ExecuteNonQuery();
                GLog.Print("end call_level_vol");
            }
            catch (MySqlException e)
            {
                GLog.Print("Error SELECT ES_CALL_LEVELS_VOL: " + e.ToString());
            }
            #endregion

            conn.Close();
        }

        public void BuildPutLevels(int cntLevel, string tradeDate, int month, int oi, int vol, double delta)
        {
            MySqlConnection conn = GetSqlConnetion();

            #region DELETE ES_PUT_LEVELS
            try
            {
                MySqlCommand cmd = new MySqlCommand("DELETE FROM es_put_levels WHERE tradedate=@tradedate", conn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", tradeDate);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Console.WriteLine("Error: " + e.ToString());
            }
            #endregion

            #region DELETE ES_PUT_LEVELS_VOL
            try
            {
                MySqlCommand cmd = new MySqlCommand("DELETE FROM es_put_levels_vol WHERE tradedate=@tradedate", conn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", tradeDate);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Console.WriteLine("Error: " + e.ToString());
            }
            #endregion

            #region SELECT ES_PUT_LEVELS
            try
            {
                MySqlCommand cmd = new MySqlCommand("SELECT * FROM es_put WHERE month=@month and tradedate=@tradedate and open_interest >= @oi and delta >= @delta ORDER BY open_interest DESC LIMIT 0,@cnt", conn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", tradeDate);
                cmd.Parameters.AddWithValue("@month", _months[month]);
                cmd.Parameters.AddWithValue("@cnt", cntLevel);
                cmd.Parameters.AddWithValue("@oi", oi);
                cmd.Parameters.AddWithValue("@delta", delta);

                GLog.Print("begin put_level month: " + _months[month]);
                GLog.Print("----tradedate: " + tradeDate);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    MySqlConnection conn1 = GetSqlConnetion();

                    while (reader.Read())
                    {
                        #region INSERT ES_PUT_LEVELS
                        try
                        {
                            GLog.Print("----strike: " + reader.GetString(1));

                            MySqlCommand cmd1 = new MySqlCommand("INSERT INTO es_put_levels (strike, tradedate, oi, oi_ch, sett_price, sett_price_ch, volume, high_contract, low_contract, open_range, level, level_high, level_low, oi_sp, delta, month) VALUES (@strike, @tradedate, @oi, @oi_ch, @sett_price, @sett_price_ch, @volume, @high_contract, @low_contract, @open_range, @level, @level_high, @level_low, @oi_sp, @delta, @month)", conn1);

                            cmd1.Prepare();
                            cmd1.Parameters.AddWithValue("@strike", reader.GetString(1));
                            cmd1.Parameters.AddWithValue("@tradedate", tradeDate);
                            cmd1.Parameters.AddWithValue("@oi", reader.GetString(4));
                            cmd1.Parameters.AddWithValue("@oi_ch", reader.GetString(5));
                            cmd1.Parameters.AddWithValue("@sett_price", reader.GetFloat(13));
                            cmd1.Parameters.AddWithValue("@sett_price_ch", reader.GetString(14));
                            cmd1.Parameters.AddWithValue("@volume", reader.GetString(3));

                            string st = reader.GetString(10);
                            int last = st.Length - 1;

                            if (st[last] == 'B')
                            {
                                cmd1.Parameters.AddWithValue("@high_contract", st.Remove(last));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@high_contract", st);
                            }

                            st = reader.GetString(11);
                            last = st.Length - 1;

                            if (st[last] == 'A')
                            {
                                cmd1.Parameters.AddWithValue("@low_contract", st.Remove(last));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@low_contract", st);
                            }

                            st = (reader.GetString(6)[0] == '*' || reader.GetString(6)[0] == '#' ? reader.GetString(6).Remove(0, 1) : reader.GetString(6));
                            last = st.Length - 1;

                            if (st[last] == 'B')
                            {
                                cmd1.Parameters.AddWithValue("@level_high", (reader.GetFloat(1) + float.Parse(st.Remove(last), CultureInfo.InvariantCulture) / 10));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@level_high", (reader.GetInt16(1) + float.Parse(st, CultureInfo.InvariantCulture) / 10));
                            }

                            st = (reader.GetString(7)[0] == '*' || reader.GetString(7)[0] == '#' ? reader.GetString(7).Remove(0, 1) : reader.GetString(7));
                            last = st.Length - 1;

                            if (st[last] == 'A')
                            {
                                cmd1.Parameters.AddWithValue("@level_low", (reader.GetFloat(1) + float.Parse(st.Remove(last), CultureInfo.InvariantCulture) / 10));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@level_low", (reader.GetInt16(1) + float.Parse(st, CultureInfo.InvariantCulture) / 10));
                            }

                            cmd1.Parameters.AddWithValue("@open_range", reader.GetFloat(12));
                            cmd1.Parameters.AddWithValue("@level", (reader.GetInt16(1) + reader.GetFloat(13) / 10));
                            cmd1.Parameters.AddWithValue("@oi_sp", reader.GetFloat(15));
                            cmd1.Parameters.AddWithValue("@delta", reader.GetFloat(2));
                            cmd1.Parameters.AddWithValue("@month", _months[month]);
                            cmd1.ExecuteNonQuery();
                        }
                        catch (MySqlException e)
                        {
                            GLog.Print("Error INSERT ES_PUT_LEVELS: " + e.ToString());
                        }
                        #endregion
                    }

                    conn1.Close();
                }

                cmd.ExecuteNonQuery();
                GLog.Print("end put_level");
            }
            catch (MySqlException e)
            {
                GLog.Print("Error SELECT ES_PUT_LEVELS: " + e.ToString());
            }
            #endregion

            #region SELECT ES_PUT_LEVELS_VOL
            try
            {
                MySqlCommand cmd = new MySqlCommand("SELECT * FROM es_put WHERE month=@month and tradedate=@tradedate and volume >= @vol and delta >= @delta ORDER BY volume DESC LIMIT 0,@cnt", conn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", tradeDate);
                cmd.Parameters.AddWithValue("@month", _months[month]);
                cmd.Parameters.AddWithValue("@cnt", cntLevel);
                cmd.Parameters.AddWithValue("@vol", vol);
                cmd.Parameters.AddWithValue("@delta", delta);

                GLog.Print("begin put_level_vol month: " + _months[month]);
                GLog.Print("----tradedate: " + tradeDate);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    MySqlConnection conn1 = GetSqlConnetion();

                    while (reader.Read())
                    {
                        #region INSERT ES_PUT_LEVELS
                        try
                        {
                            GLog.Print("----strike: " + reader.GetString(1));

                            MySqlCommand cmd1 = new MySqlCommand("INSERT INTO es_put_levels_vol (strike, tradedate, oi, oi_ch, sett_price, sett_price_ch, volume, high_contract, low_contract, open_range, level, level_high, level_low, oi_sp, delta, month) VALUES (@strike, @tradedate, @oi, @oi_ch, @sett_price, @sett_price_ch, @volume, @high_contract, @low_contract, @open_range, @level, @level_high, @level_low, @oi_sp, @delta, @month)", conn1);

                            cmd1.Prepare();
                            cmd1.Parameters.AddWithValue("@strike", reader.GetString(1));
                            cmd1.Parameters.AddWithValue("@tradedate", tradeDate);
                            cmd1.Parameters.AddWithValue("@oi", reader.GetString(4));
                            cmd1.Parameters.AddWithValue("@oi_ch", reader.GetString(5));
                            cmd1.Parameters.AddWithValue("@sett_price", reader.GetFloat(13));
                            cmd1.Parameters.AddWithValue("@sett_price_ch", reader.GetString(14));
                            cmd1.Parameters.AddWithValue("@volume", reader.GetString(3));

                            string st = reader.GetString(10);
                            int last = st.Length - 1;

                            if (st[last] == 'B')
                            {
                                cmd1.Parameters.AddWithValue("@high_contract", st.Remove(last));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@high_contract", st);
                            }

                            st = reader.GetString(11);
                            last = st.Length - 1;

                            if (st[last] == 'A')
                            {
                                cmd1.Parameters.AddWithValue("@low_contract", st.Remove(last));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@low_contract", st);
                            }

                            st = (reader.GetString(6)[0] == '*' || reader.GetString(6)[0] == '#' ? reader.GetString(6).Remove(0, 1) : reader.GetString(6));
                            last = st.Length - 1;

                            if (st[last] == 'B')
                            {
                                cmd1.Parameters.AddWithValue("@level_high", (reader.GetFloat(1) + float.Parse(st.Remove(last), CultureInfo.InvariantCulture) / 10));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@level_high", (reader.GetInt16(1) + float.Parse(st, CultureInfo.InvariantCulture) / 10));
                            }

                            st = (reader.GetString(7)[0] == '*' || reader.GetString(7)[0] == '#' ? reader.GetString(7).Remove(0, 1) : reader.GetString(7));
                            last = st.Length - 1;

                            if (st[last] == 'A')
                            {
                                cmd1.Parameters.AddWithValue("@level_low", (reader.GetFloat(1) + float.Parse(st.Remove(last), CultureInfo.InvariantCulture) / 10));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@level_low", (reader.GetInt16(1) + float.Parse(st, CultureInfo.InvariantCulture) / 10));
                            }

                            cmd1.Parameters.AddWithValue("@open_range", reader.GetFloat(12));
                            cmd1.Parameters.AddWithValue("@level", (reader.GetInt16(1) + reader.GetFloat(13) / 10));
                            cmd1.Parameters.AddWithValue("@oi_sp", reader.GetFloat(15));
                            cmd1.Parameters.AddWithValue("@delta", reader.GetFloat(2));
                            cmd1.Parameters.AddWithValue("@month", _months[month]);
                            cmd1.ExecuteNonQuery();
                        }
                        catch (MySqlException e)
                        {
                            GLog.Print("Error INSERT ES_PUT_LEVELS_VOL: " + e.ToString());
                        }
                        #endregion
                    }

                    conn1.Close();
                }

                cmd.ExecuteNonQuery();
                GLog.Print("end put_level_vol");
            }
            catch (MySqlException e)
            {
                GLog.Print("Error SELECT ES_PUT_LEVELS_VOL: " + e.ToString());
            }
            #endregion

            conn.Close();
        }

        public void BuildCallLevels(int cntLevel, string tradeDate, int month, int oi_st, int oi_end, int oi_ch, double open_range, double delta_st, double delta_end)
        {
            if (!isSqlConnetion()) return;

            #region DELETE ES_CALL_LEVELS
            try
            {
                MySqlCommand cmd = new MySqlCommand("DELETE FROM es_call_levels WHERE tradedate=@tradedate", _myConn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", tradeDate);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Console.WriteLine("Error: " + e.ToString());
            }
            #endregion

            #region SELECT ES_CALL_LEVELS
            try
            {
                String cmdText = "SELECT * FROM es_call WHERE month=@month and tradedate=@tradedate and (open_interest >= @oi_st and open_interest <= @oi_end) and ABS(open_interest_ch) >= @oi_ch and open_range >= @open_range and (delta >= @delta_st and delta <= @delta_end) ORDER BY oi_sp DESC LIMIT 0,@cnt";
                MySqlCommand cmd = new MySqlCommand(cmdText, _myConn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", tradeDate);
                cmd.Parameters.AddWithValue("@month", _months[month]);
                cmd.Parameters.AddWithValue("@cnt", cntLevel);
                cmd.Parameters.AddWithValue("@oi_st", oi_st);
                cmd.Parameters.AddWithValue("@oi_end", oi_end);
                cmd.Parameters.AddWithValue("@oi_ch", oi_ch);
                cmd.Parameters.AddWithValue("@open_range", open_range);
                cmd.Parameters.AddWithValue("@delta_st", delta_st);
                cmd.Parameters.AddWithValue("@delta_end", delta_end);

                GLog.Print("begin call_level month: " + _months[month]);
                GLog.Print("----tradedate: " + tradeDate);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {                
                    while (reader.Read())
                    {
                        #region INSERT ES_CALL_LEVELS
                        try
                        {
                            GLog.Print("----strike: " + reader.GetString(1));

                            MySqlConnection conn = GetSqlConnetion();
                            String cmdText1 = "INSERT INTO es_call_levels (strike, tradedate, oi, oi_ch, sett_price, sett_price_ch, volume, high_contract, low_contract, open_range, level, level_high, level_low, oi_sp, delta, month) VALUES (@strike, @tradedate, @oi, @oi_ch, @sett_price, @sett_price_ch, @volume, @high_contract, @low_contract, @open_range, @level, @level_high, @level_low, @oi_sp, @delta, @month)";
                            MySqlCommand cmd1 = new MySqlCommand(cmdText1, conn);

                            cmd1.Prepare();
                            cmd1.Parameters.AddWithValue("@strike", reader.GetString(1));
                            cmd1.Parameters.AddWithValue("@tradedate", tradeDate);
                            cmd1.Parameters.AddWithValue("@oi", reader.GetString(4));
                            cmd1.Parameters.AddWithValue("@oi_ch", reader.GetString(5));
                            cmd1.Parameters.AddWithValue("@sett_price", reader.GetFloat(13));
                            cmd1.Parameters.AddWithValue("@sett_price_ch", reader.GetString(14));
                            cmd1.Parameters.AddWithValue("@volume", reader.GetString(3));

                            string st = reader.GetString(10);
                            int last = st.Length - 1;

                            if (st[last] == 'B')
                            {
                                cmd1.Parameters.AddWithValue("@high_contract", st.Remove(last));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@high_contract", st);
                            }

                            st = reader.GetString(11);
                            last = st.Length - 1;

                            if (st[last] == 'A')
                            {
                                cmd1.Parameters.AddWithValue("@low_contract", st.Remove(last));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@low_contract", st);
                            }

                            st = (reader.GetString(6)[0] == '*' || reader.GetString(6)[0] == '#' ? reader.GetString(6).Remove(0, 1) : reader.GetString(6));
                            last = st.Length - 1;

                            if (st[last] == 'B')
                            {
                                cmd1.Parameters.AddWithValue("@level_high", (reader.GetFloat(1) + float.Parse(st.Remove(last), CultureInfo.InvariantCulture) / 10));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@level_high", (reader.GetInt16(1) + float.Parse(st, CultureInfo.InvariantCulture) / 10));
                            }

                            st = (reader.GetString(7)[0] == '*' || reader.GetString(7)[0] == '#' ? reader.GetString(7).Remove(0, 1) : reader.GetString(7));
                            last = st.Length - 1;

                            if (st[last] == 'A')
                            {
                                cmd1.Parameters.AddWithValue("@level_low", (reader.GetFloat(1) + float.Parse(st.Remove(last), CultureInfo.InvariantCulture) / 10));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@level_low", (reader.GetInt16(1) + float.Parse(st, CultureInfo.InvariantCulture) / 10));
                            }

                            cmd1.Parameters.AddWithValue("@open_range", reader.GetFloat(12));
                            cmd1.Parameters.AddWithValue("@level", (reader.GetInt16(1) + reader.GetFloat(13) / 10));
                            cmd1.Parameters.AddWithValue("@oi_sp", reader.GetFloat(15));
                            cmd1.Parameters.AddWithValue("@delta", reader.GetFloat(2));
                            cmd1.Parameters.AddWithValue("@month", _months[month]);
                            cmd1.ExecuteNonQuery();
                            conn.Close();
                        }
                        catch (MySqlException e)
                        {
                            GLog.Print("Error INSERT ES_CALL_LEVELS: " + e.ToString());
                        }
                        #endregion
                    }
                }

                cmd.ExecuteNonQuery();
                GLog.Print("end call_level");
            }
            catch (MySqlException e)
            {
                GLog.Print("Error SELECT ES_CALL_LEVELS: " + e.ToString());
            }
            #endregion

            SqlClose();
        }

        public void BuildPutLevels(int cntLevel, string tradeDate, int month, int oi_st, int oi_end, int oi_ch, double open_range, double delta_st, double delta_end)
        {
            if (!isSqlConnetion()) return;

            #region DELETE ES_PUT_LEVELS
            try
            {
                MySqlCommand cmd = new MySqlCommand("DELETE FROM es_put_levels WHERE tradedate=@tradedate", _myConn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", tradeDate);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Console.WriteLine("Error: " + e.ToString());
            }
            #endregion

            #region SELECT ES_PUT_LEVELS
            try
            {
                String cmdText = "SELECT * FROM es_put WHERE month=@month and tradedate=@tradedate and (open_interest >= @oi_st and open_interest <= @oi_end) and ABS(open_interest_ch) >= @oi_ch and open_range >= @open_range and (delta >= @delta_st and delta <= @delta_end) ORDER BY oi_sp DESC LIMIT 0,@cnt";
                MySqlCommand cmd = new MySqlCommand(cmdText, _myConn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", tradeDate);
                cmd.Parameters.AddWithValue("@month", _months[month]);
                cmd.Parameters.AddWithValue("@cnt", cntLevel);
                cmd.Parameters.AddWithValue("@oi_st", oi_st);
                cmd.Parameters.AddWithValue("@oi_end", oi_end);
                cmd.Parameters.AddWithValue("@oi_ch", oi_ch);
                cmd.Parameters.AddWithValue("@open_range", open_range);
                cmd.Parameters.AddWithValue("@delta_st", delta_st);
                cmd.Parameters.AddWithValue("@delta_end", delta_end);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        #region INSERT ES_PUT_LEVELS
                        try
                        {
                            MySqlConnection conn = GetSqlConnetion();
                            String cmdText1 = "INSERT INTO es_put_levels (strike, tradedate, oi, oi_ch, sett_price, sett_price_ch, volume, high_contract, low_contract, open_range, level, level_high, level_low, oi_sp, delta, month) VALUES (@strike, @tradedate, @oi, @oi_ch, @sett_price, @sett_price_ch, @volume, @high_contract, @low_contract, @open_range, @level, @level_high, @level_low, @oi_sp, @delta, @month)";
                            MySqlCommand cmd1 = new MySqlCommand(cmdText1, conn);

                            cmd1.Prepare();
                            cmd1.Parameters.AddWithValue("@strike", reader.GetString(1));
                            cmd1.Parameters.AddWithValue("@tradedate", tradeDate);
                            cmd1.Parameters.AddWithValue("@oi", reader.GetString(4));
                            cmd1.Parameters.AddWithValue("@oi_ch", reader.GetString(5));
                            cmd1.Parameters.AddWithValue("@sett_price", reader.GetFloat(13));
                            cmd1.Parameters.AddWithValue("@sett_price_ch", reader.GetString(14));
                            cmd1.Parameters.AddWithValue("@volume", reader.GetString(3));

                            string st = reader.GetString(10);
                            int last = st.Length - 1;

                            if (st[last] == 'B')
                            {
                                cmd1.Parameters.AddWithValue("@high_contract", st.Remove(last));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@high_contract", st);
                            }

                            st = reader.GetString(11);
                            last = st.Length - 1;

                            if (st[last] == 'A')
                            {
                                cmd1.Parameters.AddWithValue("@low_contract", st.Remove(last));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@low_contract", st);
                            }

                            st = (reader.GetString(6)[0] == '*' || reader.GetString(6)[0] == '#' ? reader.GetString(6).Remove(0, 1) : reader.GetString(6));
                            last = st.Length - 1;

                            if (st[last] == 'B')
                            {
                                cmd1.Parameters.AddWithValue("@level_high", (reader.GetFloat(1) - float.Parse(st.Remove(last), CultureInfo.InvariantCulture) / 10));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@level_high", (reader.GetInt16(1) - float.Parse(st, CultureInfo.InvariantCulture) / 10));
                            }

                            st = (reader.GetString(7)[0] == '*' || reader.GetString(7)[0] == '#' ? reader.GetString(7).Remove(0, 1) : reader.GetString(7));
                            last = st.Length - 1;

                            if (st[last] == 'A')
                            {
                                cmd1.Parameters.AddWithValue("@level_low", (reader.GetFloat(1) - float.Parse(st.Remove(last), CultureInfo.InvariantCulture) / 10));
                            }
                            else
                            {
                                cmd1.Parameters.AddWithValue("@level_low", (reader.GetInt16(1) - float.Parse(st, CultureInfo.InvariantCulture) / 10));
                            }

                            cmd1.Parameters.AddWithValue("@open_range", reader.GetFloat(12));
                            cmd1.Parameters.AddWithValue("@level", (reader.GetInt16(1) - reader.GetFloat(13) / 10));
                            cmd1.Parameters.AddWithValue("@oi_sp", reader.GetFloat(15));
                            cmd1.Parameters.AddWithValue("@delta", reader.GetFloat(2));
                            cmd1.Parameters.AddWithValue("@month", _months[month]);
                            cmd1.ExecuteNonQuery();
                            conn.Close();
                        }
                        catch (MySqlException e)
                        {
                            GLog.Print("Error INSERT ES_PUT_LEVELS: " + e.ToString());
                        }
                        #endregion
                    }
                }

                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                GLog.Print("Error SELECT ES_PUT_LEVELS: " + e.ToString());
            }
            #endregion

            SqlClose();
        }

        public void GetCallLevels()
        {
            if (!isSqlConnetion()) return;

            try
            {
                String cmdText = "SELECT * FROM es_call_levels";
                MySqlCommand cmd = new MySqlCommand(cmdText, _myConn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", _tradeDate);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        LEVELS_CALL_PUT lcp = new LEVELS_CALL_PUT(reader.GetInt16(1), reader.GetDateTime(2), reader.GetInt16(3), reader.GetInt16(4), reader.GetDouble(5), reader.GetInt16(6), reader.GetInt16(7), reader.GetDouble(10), reader.GetDouble(11), reader.GetDouble(12), reader.GetDouble(13));
                        Levels_Call.Add(lcp);
                    }
                }

                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Console.WriteLine("Error: " + e.ToString());
            }

            _myConn.Close();
        }

        public void GetPutLevels()
        {
            if (!isSqlConnetion()) return;

            try
            {
                String cmdText = "SELECT * FROM es_put_levels";
                MySqlCommand cmd = new MySqlCommand(cmdText, _myConn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", _tradeDate);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        LEVELS_CALL_PUT lcp = new LEVELS_CALL_PUT(reader.GetInt16(1), reader.GetDateTime(2), reader.GetInt16(3), reader.GetInt16(4), reader.GetDouble(5), reader.GetInt16(6), reader.GetInt16(7), reader.GetDouble(10), reader.GetDouble(11), reader.GetDouble(12), reader.GetDouble(13));
                        Levels_Put.Add(lcp);
                    }
                }

                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Console.WriteLine("Error: " + e.ToString());
            }

            _myConn.Close();
        }

        public List<LEVELS_CALL_PUT> GetCallLevelsDate(string date)
        {
            if (!isSqlConnetion()) return null;

            List<LEVELS_CALL_PUT> _Levels_Call = new List<LEVELS_CALL_PUT>();
      
            try
            {
                String cmdText = "SELECT * FROM es_call_levels WHERE tradedate=@tradedate";
                MySqlCommand cmd = new MySqlCommand(cmdText, _myConn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", (date == "") ? _tradeDate : date);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Levels_Call.Add(new LEVELS_CALL_PUT(reader.GetInt16(1), reader.GetDateTime(2), reader.GetInt16(3), reader.GetInt16(4), reader.GetDouble(5), reader.GetInt16(6), reader.GetInt16(7), reader.GetDouble(10), reader.GetDouble(11), reader.GetDouble(12), reader.GetDouble(13)));
                        _Levels_Call.Add(new LEVELS_CALL_PUT(reader.GetInt16(1), reader.GetDateTime(2), reader.GetInt16(3), reader.GetInt16(4), reader.GetDouble(5), reader.GetInt16(6), reader.GetInt16(7), reader.GetDouble(10), reader.GetDouble(11), reader.GetDouble(12), reader.GetDouble(13)));
                    }
                }

                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Console.WriteLine("Error: " + e.ToString());
            }

            _myConn.Close();
            return _Levels_Call;
        }

        public List<LEVELS_CALL_PUT> GetPutLevelsDate(string date)
        {
            if (!isSqlConnetion()) return null;

            List<LEVELS_CALL_PUT> _Levels_Put = new List<LEVELS_CALL_PUT>();

            try
            {
                String cmdText = "SELECT * FROM es_put_levels WHERE tradedate=@tradedate";
                MySqlCommand cmd = new MySqlCommand(cmdText, _myConn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", (date == "") ? _tradeDate : date);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Levels_Put.Add(new LEVELS_CALL_PUT(reader.GetInt16(1), reader.GetDateTime(2), reader.GetInt16(3), reader.GetInt16(4), reader.GetDouble(5), reader.GetInt16(6), reader.GetInt16(7), reader.GetDouble(10), reader.GetDouble(11), reader.GetDouble(12), reader.GetDouble(13)));
                        _Levels_Put.Add(new LEVELS_CALL_PUT(reader.GetInt16(1), reader.GetDateTime(2), reader.GetInt16(3), reader.GetInt16(4), reader.GetDouble(5), reader.GetInt16(6), reader.GetInt16(7), reader.GetDouble(10), reader.GetDouble(11), reader.GetDouble(12), reader.GetDouble(13)));
                    }
                }

                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Console.WriteLine("Error: " + e.ToString());
            }

            _myConn.Close();
            return _Levels_Put;
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
                Console.WriteLine("Error: " + e.ToString());
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
        private  MySqlConnection GetSqlConnetion()
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
        public void SqlClose()
        {
            _myConn.Close();
        }
        #endregion

        /*public static string ExtractTextFromPdf(string path)
        {
            ITextExtractionStrategy its = new iTextSharp.text.pdf.parser.LocationTextExtractionStrategy();

            using (PdfReader reader = new PdfReader(path))
            {
                StringBuilder text = new StringBuilder();

                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    string thePage = PdfTextExtractor.GetTextFromPage(reader, i, its);
                    string[] theLines = thePage.Split(new char ['\n'], 2);

                    foreach (var theLine in theLines)
                    {
                        text.AppendLine(theLine);
                    }
                }

                return text.ToString();
            }
        }

        private StringBuilder extractTextFromPdf(string path)
        {
            using (PdfReader reader = new PdfReader(path))
            {
                StringBuilder text = new StringBuilder();

                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    text.Append(PdfTextExtractor.GetTextFromPage(reader, i));
                }

                return text;
            }
        }*/
      
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

            if (regex.IsMatch(line)) return true;

            return false;
        }

        private bool isPut(string month, string line)
        {
            string str = "^" + month + @"\sMINI S&P P";
            Regex regex = new Regex(str);

            if (regex.IsMatch(line)) return true;

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
