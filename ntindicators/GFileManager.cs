#define FORCEFLUSH

using System;
using System.IO;
using System.ComponentModel;
using System.Text;

using System.Globalization;
using System.Reflection;
using System.Diagnostics;

using NinjaTrader.Data;

namespace NinjaTrader.Data
{
    public enum GFileModeType { SingleFile, OnePerDay };

    #region class GFileManager
    abstract partial class GFileManager: IGDataManager, IDisposable
    {
        #region GBackwardsReader
        public class GBackwardsReader: IDisposable
        {
            private FileStream _fs = null;

            public GBackwardsReader(string path)
            {
                _fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                _fs.Seek(0, SeekOrigin.End);
            }

            public string ReadLine()
            {
                byte[] line;
                byte[] text = new byte[1];
                long position = 0;
                int count;

                _fs.Seek(0, SeekOrigin.Current);
                position = _fs.Position;

                if (_fs.Length > 1)
                {
                    byte[] vagnretur = new byte[2];
                    
                    _fs.Seek(-2, SeekOrigin.Current);
                    _fs.Read(vagnretur, 0, 2);

                    if (ASCIIEncoding.ASCII.GetString(vagnretur).Equals("\r\n"))
                    {
                        _fs.Seek(-2, SeekOrigin.Current);
                        position = _fs.Position;
                    }
                }

                while (_fs.Position > 0)
                {
                    text.Initialize();
                    _fs.Read(text, 0, 1);

                    string asciiText = ASCIIEncoding.ASCII.GetString(text);

                    _fs.Seek(-2, SeekOrigin.Current);

                    if (asciiText.Equals("\n"))
                    {
                        _fs.Read(text, 0, 1);
                        asciiText = ASCIIEncoding.ASCII.GetString(text);

                        if (asciiText.Equals("\r"))
                        {
                            _fs.Seek(1, SeekOrigin.Current);
                            break;
                        }
                    }
                }

                count = int.Parse((position - _fs.Position).ToString());
                line = new byte[count];

                _fs.Read(line, 0, count);
                _fs.Seek(-count, SeekOrigin.Current);
                return ASCIIEncoding.ASCII.GetString(line);
            }

            public bool SOF { get { return _fs.Position == 0; } }

            protected virtual void Dispose(bool d)
            {
                if (d)
                {
                    if (_fs != null)
                    {
                        _fs.Close();
                        _fs.Dispose();
                        _fs = null;
                    }
                }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }
        #endregion

        #region Variables
        abstract public string Name { get; }
        
        virtual public bool IsWritable { get { return false; } }

        virtual public bool IsMillisecCompliant { get { return false; } }

        public const string dateFormat = "yyMMddHHmmss";
        public const string dateFormatMillisec = "yyMMddHHmmss";
        public string[] dateFormats = { dateFormat, dateFormatMillisec };

        protected CultureInfo curCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        
        protected bool useMillisec;

        private string _instrName;
        private bool _writeData;
        private string _converterFileName;
        private GFileModeType _fileMode;
        private bool _writeOK = true;

        protected long curDateGMTTicks;
        protected long newDateGMTTicks;
        protected DateTime curReadDate = GUtils.nullDT;

        protected double tickSize;
        protected bool isBinary;
        private long _lastTimeInFile;

        protected StreamReader sr;
        protected StreamWriter sw;

        protected BinaryReader br;
        protected BinaryWriter bw;

        protected string fileName = null;
        protected string folderName;
        #endregion

        #region Contructors
        public GFileManager()
        {
            curCulture.NumberFormat.NumberGroupSeparator = "";
        }

        public GFileManager(bool isInstr, string name, double tsize, bool writeData, GFileModeType mode) : this()
        {
            if (isInstr)
				_instrName = name;
			else
				_converterFileName = name;

            tickSize = tsize;
			_writeData = writeData;
            _fileMode = mode;
        }

        public GFileManager(string filename) : this()
		{
			_converterFileName = filename;
			_writeData = false;
			_fileMode = GFileModeType.SingleFile;
		}
        #endregion

        #region File Management
        private void _initWrite()
        {
            _freeWriter();

            fileName = _GetFileName(new DateTime(curDateGMTTicks), false);

            if (!File.Exists(fileName))
            {
                FileStream fs = File.Create(fileName);
                fs.Close();
            }

            if (_writeData && IsWritable)
            {
                try
                {
                    if (isBinary)
                        bw = new BinaryWriter(File.Open(fileName, FileMode.Append, FileAccess.Write, FileShare.Read));
                    else
                        sw = new StreamWriter(File.Open(fileName, FileMode.Append, FileAccess.Write, FileShare.Read));

                    _writeOK = true;
                }
                catch (IOException)
                {
                    _writeOK = false;
                }
            }

            if (_writeOK)
                _lastTimeInFile = GetMaxTimeInFile().Ticks;
            else
                _lastTimeInFile = DateTime.MaxValue.Ticks;
        }

        private bool _initRead()
        {
            bool found = false;

            _freeReader();

            if (!String.IsNullOrEmpty(fileName))
            {
                try
                {
                    if (isBinary)
                        br = new BinaryReader(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                    else
                        sr = new StreamReader(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

                    found = true;
                }
                catch (IOException)
                {
                    found = false;
                }
            }

            return (found);
        }

        private string _GetFileName(DateTime date, bool addinstrfolder)
        {
            if (_converterFileName == null)
            {
                string nomfic = _instrName;
                char[] invalidFileChars = Path.GetInvalidFileNameChars();

                // remove invalid chars (*,/,\ etx)
                foreach (char invalidFChar in invalidFileChars)
                    nomfic = nomfic.Replace(invalidFChar.ToString(), "");

                if (_fileMode == GFileModeType.OnePerDay)
                    nomfic += "." + date.ToString("yyyyMMdd");

                string ext;

                if (isBinary)
                    ext = ".dat";
                else
                    ext = ".txt";

                string folder = Environment.GetEnvironmentVariable("GFOLDER");

                if (folder == null)
                    folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (addinstrfolder)
                    folder += "\\" + _instrName;

                return (folder + "\\" + nomfic + "." + Name + ext);
            }
            else
                return _converterFileName;
        }

        private string _GetFileName(DateTime date)
        {
            string fileName = _GetFileName(date, false);

            if (!File.Exists(fileName))
                fileName = _GetFileName(date, true);

            return fileName;
        }

        private bool _FindFileNameAndOpen(DateTime date)
        {
            bool found = false;

            if (_fileMode == GFileModeType.SingleFile)
            {
                fileName = _GetFileName(GUtils.nullDT);
                found = _initRead();
            }
            else
            {
                while (date <= DateTime.Now.ToUniversalTime().Date)
                {
                    fileName = _GetFileName(date);

                    if (File.Exists(fileName))
                    {
                        FileInfo f = new FileInfo(fileName);

                        if (f.Length > 0)
                        {
                            curReadDate = date;
                            found = _initRead();
                            break;
                        }
                    }

                    date = date.AddDays(1);
                }
            }

            return found;
        }

        protected bool ManageFileChange()
        {
            bool found = false;

            _freeReader();

            if (_fileMode == GFileModeType.OnePerDay)
                found = _FindFileNameAndOpen(curReadDate.AddDays(1));

            return found;
        }
        #endregion

        #region Helpers
        protected void SwapCulture()
        {
            if (curCulture.NumberFormat.NumberDecimalSeparator == ".")
                curCulture.NumberFormat.NumberDecimalSeparator = ",";
            else
                curCulture.NumberFormat.NumberDecimalSeparator = ".";
        }

        public bool RecordTick(DateTime dt, GTickTypeEnum tickType, double price, int volume)
        {
            DateTime newDateTimeGMT = dt.ToUniversalTime();
            newDateGMTTicks = newDateTimeGMT.Date.Ticks;

            if (((_fileMode == GFileModeType.OnePerDay) && (_writeOK) && (newDateGMTTicks > curDateGMTTicks)) || (curDateGMTTicks == 0))
            {
                curDateGMTTicks = newDateGMTTicks;
                _initWrite();
            }

            if ((_writeOK) && (newDateTimeGMT.Ticks > _lastTimeInFile))
                RecordTickGMT(newDateTimeGMT, tickType, price, volume);

            return _writeOK;
        }
        #endregion

        #region IGDataManager
        public bool RecordTick(DateTime dt, double bid, double ask, double price, int volume)
        {
            return RecordTick(dt, GUtils.GetIntTickType(bid, ask, price), price, volume);
        }

        public abstract void GetNextTick(ref GMarketDataType data);

        public void SetCursorTime(DateTime time0, ref GMarketDataType data)
        {
            long time0tick = time0.Ticks;
            long ticktime;

            data.time = GUtils.nullDT;

            if (_FindFileNameAndOpen(time0.ToUniversalTime().Date))
                do
                {
                    GetNextTick(ref data);
                    ticktime = data.time.Ticks;
                }
                while ((ticktime != 0L) && (ticktime < time0tick));
        }
        #endregion

        #region Disposable
        private void _freeReader()
		{
			if (br != null)
			{
				br.Close();
				br = null;
			}

			if (sr != null)
			{
				sr.Close();
				sr.Dispose();
				sr = null;
			}
		}

        private void _freeWriter()
		{
			if (bw != null)
			{
				bw.Flush();
				bw.Close();
				bw = null;
			}

			if (sw != null)
			{
				sw.Flush();
				sw.Close();
				sw.Dispose();
				sw = null;
			}
		}

        protected virtual void Dispose(bool d)
		{
			if (d)
			{
				_freeReader();
				_freeWriter();
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
        #endregion

        virtual public void RecordTickGMT(DateTime date, GTickTypeEnum tickType, double price, int volume)
        { }

        virtual protected DateTime GetMaxTimeInFile()
        {
            return GUtils.nullDT;
        }
    }
    #endregion

    class GFileManagerFlat : GFileManager
    {
        public override string Name { get { return "Flat"; } }

        public override bool IsWritable { get { return true; } }

        public override bool IsMillisecCompliant { get { return true; } }

        public GFileManagerFlat() : base() { }

        public GFileManagerFlat(bool isInstr, string name, double tickSize, bool writedata, GFileModeType fileMode) : base(isInstr, name, tickSize, writedata, fileMode)
        { 
            useMillisec = true;
        }

        public override void RecordTickGMT(DateTime time, GTickTypeEnum tickType, double price, int volume)
        {
            sw.WriteLine(time.ToString((useMillisec) ? dateFormatMillisec : dateFormat) + "\t" + (int)tickType + "\t" + price.ToString("G10", CultureInfo.InvariantCulture) + "\t" + volume);

#if FORCEFLUSH
            sw.Flush();
#endif
        }

        private string GetNextLinePivotFormatted()
        {
            string retString = null;

            if (!sr.EndOfStream)
            {
                retString = sr.ReadLine();
            }
            return retString;
        }

        public override void GetNextTick(ref GMarketDataType data)
        {
            string retString = GetNextLinePivotFormatted();

            if (retString != null)
            {
                string[] split = retString.Split('\t');

                data.time = DateTime.ParseExact(split[0], dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None).ToLocalTime();
                data.tickType = (GTickTypeEnum)Enum.Parse(typeof(GTickTypeEnum), split[1]);

                try
                {
                    data.price = Double.Parse(split[2], curCulture);
                }
                catch (FormatException)
                {
                    SwapCulture();
                    data.price = Double.Parse(split[2], curCulture);
                }

                data.volume = Int32.Parse(split[3]);
            }
            else
            {
                data.time = GUtils.nullDT;

                if (ManageFileChange())
                    GetNextTick(ref data);
            }
        }

        protected override DateTime GetMaxTimeInFile()
        {
            DateTime retTime = new DateTime(0L);
            string stringRead;
            string lastTimeInFileString = "";

            using(GBackwardsReader br = new GBackwardsReader(fileName))
            {
                if (!br.SOF)
                {
                    stringRead = br.ReadLine();

                    if ((stringRead != null) && (stringRead.Length > 12))
                    {
                        lastTimeInFileString = stringRead.Substring(0, 12);
                        retTime = DateTime.ParseExact(lastTimeInFileString, dateFormats, CultureInfo.CurrentCulture, DateTimeStyles.None);
                    }
                }
            }

            return retTime;
        }
    }

    class GFileManagerBinary : GFileManager
    {
        public override string Name { get { return "Binary"; } }

        public override bool IsWritable { get { return true; } }

        private int _lastMinute = -1;
        private int _lastSecond;
        private double _pivotPrice;

        private DateTime _lastReadMinute = GUtils.nullDT;
        private DateTime _lastReadSecond;
        private double _lastReadPivot;

        public GFileManagerBinary() : base()
		{
			isBinary = true;
		}

		public GFileManagerBinary(bool isInstr, string name, double tickSize, bool writedata, GFileModeType fileMode) : base(isInstr, name, tickSize, writedata, fileMode)
		{
			isBinary = true;
		}

        public void WriteData(int second, GTickTypeEnum tickType, double price, int volume, bool withsecond)
        {
            Byte statbyte;
            Byte sec;
            int diff;

            if (withsecond)
                statbyte = 3 << 6; //00000011 << 6 = 11000000 (192)
            else
                statbyte = 2 << 6; //00000010 << 6 = 10000000 (128)

            statbyte += checked((Byte)((int)tickType << 3));
            diff = Convert.ToInt32(((price - _pivotPrice) / tickSize));

            if (diff >= -8 && diff <= +7 && volume <= 15)
                statbyte += 7;
            else
            {
                if ((diff > SByte.MaxValue) || (diff < SByte.MinValue))
                    statbyte += 1 << 2;

                if (volume > UInt16.MaxValue)
                    statbyte += 2;
                else if (volume > Byte.MaxValue)
                    statbyte += 1;
            }

            bw.Write(statbyte);

            if (withsecond)
            {
                sec = checked((Byte)second);
                bw.Write(sec);
            }

            if (diff >= -8 && diff <= +7 && volume <= 15)
            {
                SByte res = checked((SByte)((SByte)(diff << 4) + volume));
                bw.Write(res);
            }
            else
            {
                if ((diff > SByte.MaxValue) || (diff < SByte.MinValue))
                {
                    Int16 res = checked((Int16)diff);
                    bw.Write(res);
                }
                else
                {
                    SByte res = checked((SByte)diff);
                    bw.Write(res);
                }

                if (volume > UInt16.MaxValue)
                {
                    Int32 res = checked((Int32)volume);
                    bw.Write(res);
                }
                else if (volume > Byte.MaxValue)
                {
                    UInt16 res = checked((UInt16)volume);
                    bw.Write(res);
                }
                else
                {
                    Byte res = checked((Byte)volume);
                    bw.Write(res);
                }
            }

            _pivotPrice = price;
        }

        public override void RecordTickGMT(DateTime time, GTickTypeEnum tickType, double price, int volume)
        {
            bool newMinuteHappened = false;

            int newMinute = (time.Year - 2000) * 100000000 + time.Month * 1000000 + time.Day * 10000 + time.Hour * 100 + time.Minute;

            if (newMinute != _lastMinute)
            {
                Byte n1 = checked((Byte)1 << 6);
                n1 += checked((Byte)(newMinute % 61));
                bw.Write(n1);

                UInt32 n2 = checked((UInt32)newMinute);
                bw.Write(n2);

                UInt32 n3 = checked(Convert.ToUInt32(price / tickSize));
                bw.Write(n3);

                _lastMinute = newMinute;
                _pivotPrice = price;
                newMinuteHappened = true;
            }

            if ((time.Second != _lastSecond) || newMinuteHappened)
            {
                WriteData(time.Second, tickType, price, volume, true);
                _lastSecond = time.Second;
            }
            else
                WriteData(time.Second, tickType, price, volume, false);

#if FORCEFLUSH
			bw.Flush();
#endif
        }

        public override void GetNextTick(ref GMarketDataType data)
        {
            byte statbyte;

            data.isNewTimeStamp = GMarketDataType.GTimeStampStatus.Different;

            try
            {
                statbyte = br.ReadByte();
            }
            catch (EndOfStreamException)
            {
                data.time = GUtils.nullDT;

                if (ManageFileChange())
                    GetNextTick(ref data);

                return;
            }

            if (statbyte >> 6 == 1)
            {
                _lastReadMinute = DateTime.ParseExact(br.ReadUInt32().ToString("D10") + "00", dateFormat, CultureInfo.InvariantCulture).ToLocalTime();
                _lastReadPivot = br.ReadUInt32() * tickSize;
                GetNextTick(ref data);
                return;
            }

            else
            {
                if (statbyte >> 6 == 3)
                    _lastReadSecond = _lastReadMinute.AddSeconds(br.ReadByte());
                else
                    data.isNewTimeStamp = GMarketDataType.GTimeStampStatus.Same;


                data.tickType = (GTickTypeEnum)((statbyte & 56 /*00111000*/) >> 3);
                data.time = _lastReadSecond;

                if ((statbyte & 7 /*00000111*/ ) == 7)
                {
                    SByte toto = br.ReadSByte();

                    data.volume = toto & 15 /*00001111*/;
                    data.price = _lastReadPivot + ((SByte)(toto & 240 /*11110000*/ ) >> 4) * tickSize;
                }
                else
                {
                    if ((statbyte & 4 /*00000100*/) > 0)
                        data.price = _lastReadPivot + br.ReadInt16() * tickSize;
                    else
                        data.price = _lastReadPivot + br.ReadSByte() * tickSize;

                    if ((statbyte & 3 /*00000011*/) == 0)
                        data.volume = br.ReadByte();
                    else if ((statbyte & 3 /*00000011*/) == 1)
                        data.volume = br.ReadUInt16();
                    else if ((statbyte & 3 /*00000011*/) == 2)
                        data.volume = br.ReadInt32();
                }
            }
        }

        protected override DateTime GetMaxTimeInFile()
        {
            DateTime retTime = GUtils.nullDT;
            string readTimeInFile;

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(0, SeekOrigin.End);

                if (fs.Position == 0)
                    goto end;

                byte[] data = new byte[5];
                bool found = false;

                do
                {
                    fs.Seek(-6, SeekOrigin.Current);
                    fs.Read(data, 0, 5);

                    if (((data[0] & 192) == 64) && ((data[0] & 63) == BitConverter.ToUInt32(data, 1) % 61))

                        try
                        {
                            DateTime.ParseExact(BitConverter.ToUInt32(data, 1).ToString("D10") + "00", dateFormat, CultureInfo.CurrentCulture);
                            found = true;
                        }
                        catch (FormatException)
                        {
                        }
                }
                while ((fs.Position >= 6) && (!found));

                if (!found)
                    goto end;

                readTimeInFile = BitConverter.ToUInt32(data, 1).ToString("D10");
                int enumsize = Enum.GetValues(typeof(GTickTypeEnum)).Length;
                found = false;
                data = new byte[2];

                fs.Seek(0, SeekOrigin.End);

                do
                {
                    fs.Seek(-3, SeekOrigin.Current);
                    fs.Read(data, 0, 2);

                    if (((data[0] >> 6) == 3) && (((data[0] & 56) >> 3) <= enumsize) && (data[1] < 60))
                        found = true;

                } while ((fs.Position >= 3) && (!found));

                if (!found)
                    goto end;

                readTimeInFile += data[1].ToString("D2");
                retTime = DateTime.ParseExact(readTimeInFile, dateFormat, CultureInfo.CurrentCulture);

            }

        end:
            return retTime;
        }
    }

    class GFileManagerMillisec: GFileManager
    {
        public override string Name { get { return "Millisec"; } }

        public override bool IsWritable { get { return true; } }

        public override bool IsMillisecCompliant { get { return true; } }

        private int _lastMinute = -1;
        private int _lastTimeStamp;
        private double _pivotPrice;

        private DateTime _lastReadMinute = GUtils.nullDT;
        private DateTime _lastReadSecond;
        private double _lastReadPivot;

        public GFileManagerMillisec() : base()
		{
			isBinary = true;
		}

		public GFileManagerMillisec(bool isInstr, string name, double tickSize, bool writedata, GFileModeType fileMode) : base(isInstr, name, tickSize, writedata, fileMode)
		{
			isBinary = true;
		}

        public void WriteData(int timestamp, GTickTypeEnum tickType, double price, int volume, bool withsecond)
        {
            Byte statbyte;
            ushort sec;
            int diff;

            if (withsecond)
                statbyte = 3 << 6;
            else
                statbyte = 2 << 6;

            statbyte += checked((Byte)((int)tickType << 3));
            diff = Convert.ToInt32(((price - _pivotPrice) / tickSize));

            if (diff >= -8 && diff <= +7 && volume <= 15)
                statbyte += 7;
            else
            {
                if ((diff > SByte.MaxValue) || (diff < SByte.MinValue))
                    statbyte += 1 << 2;

                if (volume > UInt16.MaxValue)
                    statbyte += 2;
                else if (volume > Byte.MaxValue)
                    statbyte += 1;
            }

            bw.Write(statbyte);

            if (withsecond)
            {
                sec = checked((UInt16)(timestamp));
                bw.Write(sec);
            }

            if (diff >= -8 && diff <= +7 && volume <= 15)
            {
                SByte res = checked((SByte)((SByte)(diff << 4) + volume));
                bw.Write(res);
            }
            else
            {
                if ((diff > SByte.MaxValue) || (diff < SByte.MinValue))
                {
                    Int16 res = checked((Int16)diff);
                    bw.Write(res);
                }
                else
                {
                    SByte res = checked((SByte)diff);
                    bw.Write(res);
                }

                if (volume > UInt16.MaxValue)
                {
                    Int32 res = checked((Int32)volume);
                    bw.Write(res);
                }
                else if (volume > Byte.MaxValue)
                {
                    UInt16 res = checked((UInt16)volume);
                    bw.Write(res);
                }
                else
                {
                    Byte res = checked((Byte)volume);
                    bw.Write(res);
                }
            }
            
            _pivotPrice = price;
        }

        public override void RecordTickGMT(DateTime time, GTickTypeEnum tickType, double price, int volume)
        {
            bool newMinuteHappened = false;
            int newMinute = Int32.Parse(time.ToString("yyMMddHHmm"));

            if (newMinute != _lastMinute)
            {
                Byte n1 = checked((Byte)1 << 6);
                n1 += checked((Byte)(newMinute % 61));
                bw.Write(n1);

                UInt32 n2 = checked((UInt32)newMinute);
                bw.Write(n2);

                UInt32 n3 = checked(Convert.ToUInt32(price / tickSize));
                bw.Write(n3);

                _lastMinute = newMinute;
                _pivotPrice = price;
                newMinuteHappened = true;
            }

            int nextTimeStamp = time.Second * 1000 + time.Millisecond;

            if ((nextTimeStamp != _lastTimeStamp) || newMinuteHappened)
            {
                WriteData(nextTimeStamp, tickType, price, volume, true);
                _lastTimeStamp = nextTimeStamp;
            }
            else
                WriteData(nextTimeStamp, tickType, price, volume, false);

#if FORCEFLUSH
			bw.Flush();
#endif
        }

        public override void GetNextTick(ref GMarketDataType data)
        {
            bool EOS = false;
            byte statbyte;

            try
            {
                statbyte = br.ReadByte();
            }
            catch (EndOfStreamException)
            {
                data.time = GUtils.nullDT;
                EOS = true;
                goto end;
            }

            if (statbyte >> 6 == 1)
            {
                _lastReadMinute = DateTime.ParseExact(br.ReadUInt32().ToString("D10") + "00", dateFormat, CultureInfo.InvariantCulture).ToLocalTime();
                _lastReadPivot = br.ReadUInt32() * tickSize;
                GetNextTick(ref data);
                return;
            }
            else
            {
                if (statbyte >> 6 == 3)
                    _lastReadSecond = _lastReadMinute.AddMilliseconds(br.ReadUInt16());

                data.tickType = (GTickTypeEnum)((statbyte & 56 /*00111000*/) >> 3);
                data.time = _lastReadSecond;

                if ((statbyte & 7 /*00000111*/ ) == 7)
                {
                    SByte toto = br.ReadSByte();
                    data.volume = toto & 15 /*00001111*/;
                    data.price = _lastReadPivot + ((SByte)(toto & 240 /*11110000*/ ) >> 4) * tickSize;
                }
                else
                {
                    if ((statbyte & 4 /*00000100*/) > 0)
                        data.price = _lastReadPivot + br.ReadInt16() * tickSize;
                    else
                        data.price = _lastReadPivot + br.ReadSByte() * tickSize;

                    if ((statbyte & 3 /*00000011*/) == 0)
                        data.volume = br.ReadByte();
                    else if ((statbyte & 3 /*00000011*/) == 1)
                        data.volume = br.ReadUInt16();
                    else if ((statbyte & 3 /*00000011*/) == 2)
                        data.volume = br.ReadInt32();
                }

                _lastReadPivot = data.price;
            }

        end:
            if (EOS)
            {
                if (ManageFileChange())
                    GetNextTick(ref data);
            }
        }

        protected override DateTime GetMaxTimeInFile()
        {
            return GUtils.nullDT;
        }
    }
}