using System;
using System.Drawing;
using System.ComponentModel;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Indicator;

namespace NinjaTrader.Indicator
{
    /// <summary>
    /// Base recorder indicator class
    /// </summary>
    [Description("Base recorder indicator class")]
    public partial class GRecorderIndicator : Indicator
    {
        #region Variables
        private double _ask = Double.MinValue;
        private double _bid = Double.MaxValue;
        private bool _initBidAsk = false;
        private bool _foundBarInFile = false;

        private bool _EOFfound = false;

        private int _maxVolume = Int32.MaxValue;
        private int _maxTick = Int32.MaxValue;

        private long _beginTimeTicks;
        private long _endTimeTicks;

        protected string recordingMessage { get { return _GetRecordingMessage(); } }

        private int _curBarVol;
        private int _curBarTicks;
        private int _volOverflow;

        private int _timeOffset;

        private Queue<GMarketDataType> _tickQueue = new Queue<GMarketDataType>();

        private IGDataManager _fm;
        private GMarketDataType _mktData;
        private long _mktDataTimeTicks;
        private bool _fetchOK;

        private string _fileFormat;
        private string _folderName;
        private int _iDataManager = -1;

        private bool _writable;
        private bool _millisecCompliant;

        private bool _splitVolume = true;
        private bool _disableTime;
        private bool _writeData;
        private bool _writeOK = true;

        private bool _firstRec;
        private int _vol2Send;
        private double _priceOffset = 0.0f;

        private DateTime _curCM = new DateTime(0L);
        private bool _dataManagerNotDisposedOf = true;
        #endregion

        #region Helpers
        private string _GetRecordingMessage()
        {
            string recmes;

            if (_writeData)
                recmes = "Recording " + _fileFormat + " " + ((!_firstRec) ? "NotNeeded" : (_writeOK) ? "OK" : "KO");
            else
                recmes = "Using " + _fileFormat;

            if (useMillisec)
                recmes += " - Lag=" + (-_timeOffset) + " ms";

            return recmes;
        }

        #region Time calc
        private void _UpdateBeginEndTime()
        {
            DateTime BeginTime, EndTime, time;

            if (_mktDataTimeTicks > _endTimeTicks)
            {
                time = new DateTime(_mktDataTimeTicks);
                Bars.Session.GetNextBeginEnd(time, out BeginTime, out EndTime);
            }
            else
            {
                time = new DateTime(_mktDataTimeTicks);
                Bars.Session.GetNextBeginEnd(time.AddSeconds(1), out BeginTime, out EndTime);
            }

            _beginTimeTicks = BeginTime.Ticks;
            _endTimeTicks = EndTime.Ticks;
        }

        private void _ComputeTimeOffset(DateTime tickTime)
        {
            DateTime t = DateTime.Now;
            DateTime correctedtime = t.AddMilliseconds(_timeOffset);
            TimeSpan diff = (correctedtime - tickTime);

            if (diff.Seconds > 0)
            {
                if (diff.Seconds > 1)
                    _timeOffset -= (diff.Seconds - 1) * 1000;

                _timeOffset -= (diff.Milliseconds + 1);
            }

            if (diff.Ticks < 0L)
            {
                _timeOffset -= (diff.Seconds * 1000 + diff.Milliseconds);

                if (t.AddMilliseconds(_timeOffset) < tickTime)
                    _timeOffset++;
            }
        }
        #endregion

        #region Dynamic PropertyGrid
        protected void ChangeProvider()
        {
            _fileFormat = GDataManagerList.Name[_iDataManager];
            _writable = GDataManagerList.Writable[_iDataManager];

            if (!_writable)
                _writeData = false;

            _millisecCompliant = GDataManagerList.MillisecCompliant[_iDataManager];
            useMillisec = _millisecCompliant;

            //custom properties
            var props = from p in typeof(GRecorderIndicator).GetProperties()
                        where p.GetCustomAttributes(typeof(SpecificTo), false).Length > 0
                        select new { PropertyName = p.Name, RecorderName = ((SpecificTo)(p.GetCustomAttributes(typeof(SpecificTo), false)[0])).Name };

            foreach (var p in props)
            {
                PropertyDescriptor descriptor = TypeDescriptor.GetProperties(this.GetType())[p.PropertyName];
                bool browsableFound = false;

                foreach (var att in descriptor.Attributes)
                {
                    if (att.GetType() == typeof(BrowsableAttribute))
                    {
                        browsableFound = true;
                        break;
                    }
                }

                if (browsableFound)
                {
                    BrowsableAttribute browsable = (BrowsableAttribute)descriptor.Attributes[typeof(BrowsableAttribute)];
                    FieldInfo isBrowsable = browsable.GetType().GetField("browsable", BindingFlags.NonPublic | BindingFlags.Instance);

                    if (!p.RecorderName.Contains(_fileFormat))
                        isBrowsable.SetValue(browsable, false);

                    else
                        isBrowsable.SetValue(browsable, true);
                }
            }

            //writeData only if writable
            PropertyDescriptor descriptorwd = TypeDescriptor.GetProperties(this.GetType())["WriteData"];
            ReadOnlyAttribute roattrib = (ReadOnlyAttribute)descriptorwd.Attributes[typeof(ReadOnlyAttribute)];
            FieldInfo isReadOnly = roattrib.GetType().GetField("isReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);

            if (!_writable)
            {
                WriteData = false;
                isReadOnly.SetValue(roattrib, true);
            }
            else
                isReadOnly.SetValue(roattrib, false);

            //usemillisec only if millisec compliant
            PropertyDescriptor descriptorum = TypeDescriptor.GetProperties(this.GetType())["UseMillisec"];
            BrowsableAttribute browsableum = (BrowsableAttribute)descriptorum.Attributes[typeof(BrowsableAttribute)];
            FieldInfo fiUseMillisec = browsableum.GetType().GetField("browsable", BindingFlags.NonPublic | BindingFlags.Instance);

            if (_millisecCompliant)
                fiUseMillisec.SetValue(browsableum, true);
            else
                fiUseMillisec.SetValue(browsableum, false);
        }
        #endregion

        #region Tick Managment
        private void _SendAll()
        {
            if (!Historical)
                _mktData.isNewTimeStamp = GMarketDataType.GTimeStampStatus.Unknown;

            GOnMarketDataWithTime(_mktData.time, _mktData.tickType, _mktData.price, _vol2Send, FirstTickOfBar);
            GOnMarketData(_mktData.tickType, _mktData.price, _vol2Send, FirstTickOfBar);
            GOnMarketData(_mktData.tickType, _mktData.price, _vol2Send);
            GOnMarketData(_mktData);
        }

        private void _SendMarketData()
        {
            if (_vol2Send != _mktData.volume)
            {
                int volold = _mktData.volume;

                _mktData.volume = _vol2Send;
                _SendAll();
                _mktData.volume = volold - _vol2Send;
            }
            else
            {
                _SendAll();
                _mktData.volume = 0;
            }

            _curBarTicks++;
            _curBarVol += _vol2Send;
        }

        private void _SetCursorTime(DateTime time0)
        {
            _fm.SetCursorTime(time0, ref _mktData);
            _mktDataTimeTicks = _mktData.time.Ticks;
            _fetchOK = (_mktDataTimeTicks != 0L);
        }

        private void _GetNextTick()
        {
            _fm.GetNextTick(ref _mktData);
            _mktData.price += _priceOffset;
            _mktDataTimeTicks = _mktData.time.Ticks;
            _fetchOK = (_mktDataTimeTicks != 0L);
        }

        private void _OnBarUpdateHistorical()
        {
            double HighPrice = High[0];
            double LowPrice = Low[0];
            DateTime time0 = Time[0];
            long roundeddticks, time0ticks;

            _curBarTicks = 0;
            _fetchOK = false;

            if (_volOverflow > _maxVolume)
            {
                _curBarVol = _maxVolume;
                _volOverflow -= _maxVolume;
            }
            else
            {
                _curBarVol = _volOverflow;
                _volOverflow = 0;
            }

            if ((BarsPeriod.Id == PeriodType.Second && BarsPeriod.Value > 1) || BarsPeriod.Id == PeriodType.Minute)
                time0 = time0.AddSeconds(-1);

            time0ticks = time0.Ticks;

            if (!_EOFfound)
            {
                do
                {
                    if (!_foundBarInFile) 	// first iteration : position cursor in file
                    {
                        DateTime begdate, dummydate;

                        Bars.Session.GetNextBeginEnd(Time[0], out begdate, out dummydate);
                        _SetCursorTime(begdate);
                        _foundBarInFile = true;
                    }
                    else if (_mktData.volume > 0)
                        _fetchOK = true;
                    else
                    {
                        do
                        {
                            _GetNextTick();

                            if ((_mktDataTimeTicks >= _endTimeTicks) && _fetchOK)
                                _UpdateBeginEndTime();

                        } while (!_disableTime && ((_mktDataTimeTicks < _beginTimeTicks) || (_mktDataTimeTicks >= _endTimeTicks)) && _fetchOK);
                    }

                    if (_fetchOK)
                    {
                        _vol2Send = _mktData.volume;
                        roundeddticks = _mktDataTimeTicks;

                        if (_millisecCompliant)
                            roundeddticks = _mktData.time.AddMilliseconds(-_mktData.time.Millisecond).Ticks;

                        if (roundeddticks < time0ticks)
                            _SendMarketData();

                        else if ((roundeddticks == time0ticks) && (_curBarTicks < _maxTick) && (_mktData.price <= HighPrice) && (_mktData.price >= LowPrice) && (_curBarVol < _maxVolume))
                        {
                            if ((!_splitVolume) || (BarsPeriod.Id != PeriodType.Volume))
                            {
                                _SendMarketData();
                                _volOverflow = Math.Max(0, _curBarVol - _maxVolume);
                            }
                            else
                            {
                                _vol2Send = Math.Min(_mktData.volume, Math.Max((_maxVolume - _curBarVol), 0));
                                _SendMarketData();
                            }
                        }
                    }
                } while ((_mktData.volume == 0) && _fetchOK);


                if (!_fetchOK)
                    _EOFfound = true;
            }
        }

        private void _OnBarUpdateRealTime()
        {
            if (FirstTickOfBar)
                _curBarVol = 0;

            //now we empty the tick queue used in OnMarketData
            int queueCount = _tickQueue.Count;

            // if COBC=true then last tick belongs to next bar
            if (CalculateOnBarClose)
                queueCount--;

            GMarketDataType tcTemp;

            for (int i = 0; i < queueCount; i++)
            {
                _mktData = _tickQueue.Dequeue();

                //not the same process if we have to split volume or not.
                //if we don't, we send all volume
                //we only split on volume chart, if volume is too high, if it is the last tick of the queue and if we asked for splitting

                if ((BarsPeriod.Id != PeriodType.Volume) || ((_curBarVol + _mktData.volume) <= _maxVolume) || (i < (queueCount - 1)) || !_splitVolume)
                {
                    _vol2Send = _mktData.volume;
                    _SendMarketData();
                }
                else
                {
                    //split volume 						
                    _vol2Send = Math.Max(_maxVolume - _curBarVol, 0);
                    _SendMarketData();//Math.Max(MaxVolume - curBarVol, 0), FirstTickOfBar);

                    //requeue remaining volume
                    //if COBC=true we have to remove the last tick or we will have an ordering problem
                    if (CalculateOnBarClose)
                    {
                        tcTemp = _tickQueue.Dequeue();
                        _tickQueue.Enqueue(_mktData);
                        _tickQueue.Enqueue(tcTemp);
                    }
                    else
                        _tickQueue.Enqueue(_mktData);
                }
            }
        }

        private void _RolloverContract()
        {
            string instrname;
            bool rollover = false;
            DateTime zonedate = TimeZoneInfo.ConvertTime(Time[0], Bars.Session.TimeZoneInfo);
            MasterInstrument MI = Bars.Instrument.MasterInstrument;
            IEnumerable<RollOver> ROCollection = MI.RollOverCollection.Cast<RollOver>();
            DateTime cm = ROCollection.Where(x => x.Date <= zonedate.Date).OrderByDescending(x => x.Date).First().ContractMonth;

            if (cm != _curCM)
            {
                rollover = true;
                _curCM = cm;
            }

            if (rollover)
            {
                _priceOffset = 0.0f;

                if (MI.MergePolicy == MergePolicy.MergeBackAdjusted)
                    _priceOffset = ROCollection.Where(x => (x.ContractMonth > cm) && (x.ContractMonth <= Instrument.Expiry) && !(double.IsNaN(x.Offset))).Sum(x => x.Offset);

                _foundBarInFile = false;
                _EOFfound = false;

                instrname = MI.Name + " " + cm.Month.ToString("D2") + "-" + (cm.Year % 100).ToString("D2");
                _fm.Initialize(instrname, _writeData, this);
            }
        }
        #endregion
        #endregion

        /// <summary>
        /// This method is used to configure the indicator and is called once before any bar data is loaded.
        /// </summary>
        protected override sealed void Initialize()
        {
            CalculateOnBarClose = false;
            BarsRequired = 0;

            GInitialize();

            if (_iDataManager == -1)
            {
                _iDataManager = GDataManagerList.Name.IndexOf("Binary");

                if (_iDataManager == -1)
                    if (GDataManagerList.Name.Count > 0)
                        _iDataManager = 0;

                if (_iDataManager > -1)
                    FileFormat = GDataManagerList.Name[_iDataManager];
            }
        }

        protected override sealed void OnStartUp()
        {
            if (BarsPeriod.Id == PeriodType.Volume)
                _maxVolume = BarsPeriod.Value;

            if (BarsPeriod.Id == PeriodType.Tick)
                _maxTick = BarsPeriod.Value;

            _fm = (IGDataManager)Activator.CreateInstance(GDataManagerList.Type[_iDataManager]);
            _fm.Initialize(BarsArray[0].Instrument.FullName, _writeData, this);

            GOnStartUp();
        }

        protected override sealed void OnBarUpdate()
        {
            GOnBarUpdate();

            if (BarsInProgress == 0)
            {
                if (Historical)
                {
                    MasterInstrument MI = Bars.Instrument.MasterInstrument;

                    if ((Bars.FirstBarOfSession || (BarsPeriod.Id == PeriodType.Day)) && (MI.InstrumentType == InstrumentType.Future) && (MI.MergePolicy != MergePolicy.DoNotMerge) && !_writeData && !Bars.Instrument.FullName.Contains("##-##"))
                        _RolloverContract();

                    _OnBarUpdateHistorical();
                }

                else
                {
                    if (_dataManagerNotDisposedOf)
                    {
                        if (!_writeData)
                        {
                            _fm.Dispose();
                            _fm = null;
                            _dataManagerNotDisposedOf = false;
                        }

                    }

                    _OnBarUpdateRealTime();
                }

                GOnBarUpdateDone();
            }
        }

        protected override sealed void OnMarketData(MarketDataEventArgs e)
        {
            DateTime t;

            if (useMillisec)
            {
                _ComputeTimeOffset(e.Time);
                t = DateTime.Now.AddMilliseconds(_timeOffset);
            }
            else
                t = e.Time;

            if (!_initBidAsk)
                _initBidAsk = (_ask > _bid);

            if ((e.MarketDataType == MarketDataType.Last))
            {
                _mktDataTimeTicks = e.Time.Ticks;

                if (_writeData && _writeOK && _writable && _initBidAsk)
                {
                    _writeOK = _fm.RecordTick(t, _bid, _ask, e.Price, (int)e.Volume);
                    _firstRec = true;
                }

                _UpdateBeginEndTime();

                if (_disableTime || ((_mktDataTimeTicks >= _beginTimeTicks) && (_mktDataTimeTicks < _endTimeTicks)))
                    _tickQueue.Enqueue(new GMarketDataType(t, GUtils.GetIntTickType(_bid, _ask, e.Price), e.Price, (int)e.Volume));
            }
            else if (e.MarketDataType == MarketDataType.Ask)
                _ask = e.Price;

            else if (e.MarketDataType == MarketDataType.Bid)
                _bid = e.Price;
        }

        public override void Plot(Graphics graphics, Rectangle bounds, double min, double max)
        {
            base.Plot(graphics, bounds, min, max);

            Color ColorNeutral = Color.FromArgb(255, 255 - ChartControl.BackColor.R, 255 - ChartControl.BackColor.G, 255 - ChartControl.BackColor.B);

            using (SolidBrush brush = new SolidBrush(ColorNeutral))
            using (StringFormat SF = new StringFormat())
            
            graphics.DrawString(recordingMessage, ChartControl.Font, brush, bounds.Left, bounds.Bottom - 22, SF);
        }

        protected override sealed void OnTermination()
        {
            GOnTermination();

            if (_fm != null)
            {
                _fm.Dispose();
                _fm = null;
            }
        }

        #region Legacy Methods
        protected virtual void GInitialize()
        { }

        protected virtual void GOnStartUp()
        { }

        protected virtual void GOnBarUpdate()
        { }

        protected virtual void GOnTermination()
        { }

        protected virtual void GOnBarUpdateDone()
        { }

        protected virtual void GOnMarketData(GMarketDataType e)
        { }

        protected virtual void GOnMarketData(GTickTypeEnum tickType, double price, int volume)
        { }

        protected virtual void GOnMarketData(GTickTypeEnum tickType, double price, int volume, bool firstTickOfBar)
        { }

        protected virtual void GOnMarketDataWithTime(DateTime tickTime, GTickTypeEnum tickType, double price, int volume, bool firstTickOfBar)
        { }
        #endregion

        #region Properties
        [Description("Путь где лежат файлы с данными")]
        [Category("Настройки : Запись данных")]
        [Gui.Design.DisplayName("Путь до папки с данными")]
        public string FolderName
        {
            get { return _folderName; }
            set { _folderName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); }
        }

        [Description("Формат файла для записи")]
        [Category("Настройки : Запись данных")]
        [Gui.Design.DisplayName("Формат файла")]
        [Browsable(true)]
        public string FileFormat
        {
            get
            {
                ChangeProvider();
                return _fileFormat;
            }
            set
            {
                _iDataManager = GDataManagerList.Name.IndexOf(value);
                ChangeProvider();
            }
        }
		
		[Description("Упаковка тиков на секудном интервале")]
        [Category("Настройки : Запись данных")]
        [Gui.Design.DisplayName("Use Millisec")]
        [Browsable(true)]
        public bool UseMillisec
        {
            get { return useMillisec; }
            set
            {
                if (_millisecCompliant)
                    useMillisec = value;
                else
                    useMillisec = false;
            }
        }
		
		[Description("Записать данные в файл")]
        [Category("Настройки : Запись данных")]
        [Gui.Design.DisplayName("Записывать данные")]
        [Browsable(true)]
        public bool WriteData
        {
            get { return _writeData; }
            set
            {
                if (_writable)
                    _writeData = value;
                else
                    _writeData = false;
            }
        }
		
		[Description("Выключение Time Filter")]
        [Category("Настройки : Запись данных")]
        [Gui.Design.DisplayName("Выкл. Time Filter")]
        [Browsable(true)]
        public bool DisableTime
        {
            get { return _disableTime; }
            set { _disableTime = value; }
        }

        [Description("Split Volume on constant volume bars")]
        [Category("Settings : Recorder")]
        [Gui.Design.DisplayName("Split Volume")]
        [Browsable(true)]
        public bool SplitVolume
        {
            get { return _splitVolume; }
            set { _splitVolume = value; }
        }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.
// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    public partial class Indicator : IndicatorBase
    {
        private GRecorderIndicator[] cacheGRecorderIndicator = null;

        private static GRecorderIndicator checkGRecorderIndicator = new GRecorderIndicator();

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public GRecorderIndicator GRecorderIndicator(string fileFormat)
        {
            return GRecorderIndicator(Input, fileFormat);
        }

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public GRecorderIndicator GRecorderIndicator(Data.IDataSeries input, string fileFormat)
        {
            if (cacheGRecorderIndicator != null)
                for (int idx = 0; idx < cacheGRecorderIndicator.Length; idx++)
                    if (cacheGRecorderIndicator[idx].EqualsInput(input))
                        return cacheGRecorderIndicator[idx];

            lock (checkGRecorderIndicator)
            {
                checkGRecorderIndicator.FileFormat = fileFormat;
                fileFormat = checkGRecorderIndicator.FileFormat;

                if (cacheGRecorderIndicator != null)
                    for (int idx = 0; idx < cacheGRecorderIndicator.Length; idx++)
                        if (cacheGRecorderIndicator[idx].EqualsInput(input))
                            return cacheGRecorderIndicator[idx];

                GRecorderIndicator indicator = new GRecorderIndicator();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.FileFormat = fileFormat;
                Indicators.Add(indicator);
                indicator.SetUp();

                GRecorderIndicator[] tmp = new GRecorderIndicator[cacheGRecorderIndicator == null ? 1 : cacheGRecorderIndicator.Length + 1];
                if (cacheGRecorderIndicator != null)
                    cacheGRecorderIndicator.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cacheGRecorderIndicator = tmp;
                return indicator;
            }
        }
    }
}

// This namespace holds all market analyzer column definitions and is required. Do not change it.
namespace NinjaTrader.MarketAnalyzer
{
    public partial class Column : ColumnBase
    {
        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.GRecorderIndicator GRecorderIndicator(string fileFormat)
        {
            return _indicator.GRecorderIndicator(Input, fileFormat);
        }

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public Indicator.GRecorderIndicator GRecorderIndicator(Data.IDataSeries input, string fileFormat)
        {
            return _indicator.GRecorderIndicator(input, fileFormat);
        }
    }
}

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    public partial class Strategy : StrategyBase
    {
        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.GRecorderIndicator GRecorderIndicator(string fileFormat)
        {
            return _indicator.GRecorderIndicator(Input, fileFormat);
        }

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public Indicator.GRecorderIndicator GRecorderIndicator(Data.IDataSeries input, string fileFormat)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.GRecorderIndicator(input, fileFormat);
        }
    }
}
#endregion
