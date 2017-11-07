using System;
using System.Text.RegularExpressions;
using System.Globalization;

using MySql.Data.MySqlClient;

namespace NinjaTrader.Data
{
    public partial class GTxtParser
    {
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
                                GLog.Print("--PARSER: SELECT ES_EO " + _tradeDate + "_" + ss);
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

                                        if (tstr[10] == "----")
                                        {
                                            cmd.Parameters.AddWithValue("@open_interest", 0);
                                            cmd.Parameters.AddWithValue("@open_interest_ch", tstr[11]);
                                            cmd.Parameters.AddWithValue("@high_contract", tstr[12]);
                                            cmd.Parameters.AddWithValue("@low_contract", tstr[13]);
                                        }
                                        else
                                        {
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
                                        }

                                        cmd.ExecuteNonQuery();
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
    }
}
