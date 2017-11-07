using System;
using System.IO;
using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace NinjaTrader.Data
{
    public partial class GTxtParser
    {
        #region Variables
        private MySqlConnection _myConn;

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

        private List<string> _months;
        private Dictionary<string, string[]> _diCall;
        private Dictionary<string, string[]> _diPut;
        private Dictionary<string, string[]> _diIndex;

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

        public struct LEVELS_INDEX
        {
            public DateTime tradedate;
            public uint oi;
            public int oi_ch;
            public uint volume;
            public double pit_high;
            public double pit_low;
            public double pit_open_range;

            public LEVELS_INDEX(DateTime tradedate, uint oi, int oi_ch, uint volume, double pit_high, double pit_low, double pit_open_range)
            {
                this.tradedate = tradedate;
                this.oi = oi;
                this.oi_ch = oi_ch;
                this.volume = volume;
                this.pit_high = Math.Round(pit_high, 2);
                this.pit_low = Math.Round(pit_low, 2);
                this.pit_open_range = Math.Round(pit_open_range, 2);
            }
        }

        public List<LEVELS_CALL_PUT> Levels_Call;
        public List<LEVELS_CALL_PUT> Levels_Put;
        public List<LEVELS_INDEX> Levels_Index;
        public Dictionary<int, DateTime> Dates;
        #endregion
    }
}
