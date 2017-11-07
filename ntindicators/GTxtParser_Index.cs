using System;
using System.Collections.Generic;
using System.Globalization;
using MySql.Data.MySqlClient;

namespace NinjaTrader.Data
{
    public partial class GTxtParser
    {
        public List<LEVELS_INDEX> GetIndexLevelsDate(string date)
        {
            if (!isSqlConnetion()) return null;

            List<LEVELS_INDEX> _Levels_Index = new List<LEVELS_INDEX>();

            try
            {
                String cmdText = "SELECT * FROM es_index WHERE tradedate=@tradedate ORDER BY open_interest DESC";
                MySqlCommand cmd = new MySqlCommand(cmdText, _myConn);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@tradedate", (date == "") ? _tradeDate : date);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        double high = double.Parse(reader.GetString(8).Replace("#", "").Replace("----", "0.0"), CultureInfo.InvariantCulture);
                        double low = double.Parse(reader.GetString(10).Replace("#", "").Replace("----", "0.0"), CultureInfo.InvariantCulture);
                        double range = double.Parse(reader.GetString(12).Replace("#", "").Replace("----", "0.0"), CultureInfo.InvariantCulture);

                        Levels_Index.Add(new LEVELS_INDEX(reader.GetDateTime(2), reader.GetUInt32(4), reader.GetInt32(5), reader.GetUInt32(3), high, low, range));
                        _Levels_Index.Add(new LEVELS_INDEX(reader.GetDateTime(2), reader.GetUInt32(4), reader.GetInt32(5), reader.GetUInt32(3), high, low, range));
                    }
                }

                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                GLog.Print("Error GetIndexLevelsDate: " + e.ToString());
            }

            _myConn.Close();
            return _Levels_Index;
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
                    for (int k = 0; k < 3; k++)
                    {
                        i++;

                        #region INSERT ES_INDEX
                        string[] tstr = _strIndex[i].Split(' ');
                        GLog.Print("--PARSER: INSERT ES_INDEX " + _strIndex[i]);
                        try
                        {
                            String cmdText = "INSERT INTO es_index (month, tradedate, volume, open_interest, open_interest_ch, high_contract, low_contract, pit_high, pit_high_a_b, pit_low, pit_low_a_b, pit_open_range) VALUES (@month, @tradedate, @volume, @open_interest, @open_interest_ch, @high_contract, @low_contract, @pit_high, @pit_high_a_b, @pit_low, @pit_low_a_b, @pit_open_range)";
                            MySqlCommand cmd = new MySqlCommand(cmdText, _myConn);

                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@month", tstr[0]);
                            cmd.Parameters.AddWithValue("@tradedate", _tradeDate);
                            cmd.Parameters.AddWithValue("@pit_open_range", tstr[1]);

                            string s = tstr[2];
                            string t = "";

                            if (s.Contains("."))
                            {
                                if (s.Contains("A"))
                                {
                                    t = s.Remove(7);
                                    cmd.Parameters.AddWithValue("@pit_high_a_b", "A");
                                }
                                else if (s.Contains("B"))
                                {
                                    t = s.Remove(7);
                                    cmd.Parameters.AddWithValue("@pit_high_a_b", "B");
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue("@pit_high_a_b", "-");
                                }
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@pit_high_a_b", "-");
                            }


                            cmd.Parameters.AddWithValue("@pit_high", ((t != "") ? t : s));

                            string s1 = tstr[3];
                            string t1 = "";

                            if (s1.Contains("."))
                            {
                                if (s1.Contains("A"))
                                {
                                    t1 = s1.Remove(7);
                                    cmd.Parameters.AddWithValue("@pit_low_a_b", "A");
                                }
                                else if (s1.Contains("B"))
                                {
                                    t1 = s1.Remove(7);
                                    cmd.Parameters.AddWithValue("@pit_low_a_b", "B");
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue("@pit_low_a_b", "-");
                                }
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@pit_low_a_b", "-");
                            }

                            cmd.Parameters.AddWithValue("@pit_low", ((t1 != "") ? t1 : s1));

                            cmd.Parameters.AddWithValue("@volume", (tstr[8] == "----") ? "0" : tstr[8]);

                            if (tstr[10] == "----")
                            {
                                cmd.Parameters.AddWithValue("@open_interest", 0);
                                cmd.Parameters.AddWithValue("@open_interest_ch", tstr[11] + tstr[12]);
                                cmd.Parameters.AddWithValue("@high_contract", tstr[13]);
                                cmd.Parameters.AddWithValue("@low_contract", tstr[14]);
                            }
                            else if (tstr[10] == "-")
                            {
                                cmd.Parameters.AddWithValue("@open_interest", "0");
                                cmd.Parameters.AddWithValue("@open_interest_ch", tstr[10] + tstr[11]);
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
    }
}
