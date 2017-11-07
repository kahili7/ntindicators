using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;

using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Indicator;

namespace NinjaTrader.Indicator
{
    #region Enum 
    public enum GCDCalculationModeType
    {
	    BidAsk,
	    UpDownTick,
	    UpDownTickWithContinuation,
	    Hybrid,
	    UpDownOneTickWithContinuation
    }

    public enum GCDChartType
    {
	    CumulativeChart,
	    NonCumulativeChart,
    }

    public enum GFilterModeType
    {
	    OnlyLargerThan,
	    OnlySmallerThan,
	    None
    }
    #endregion

	/// <summary>
    /// GDeltaIndicator
    /// </summary>
    [Description("Base Class. Do not instantiate")]
	public class GDeltaIndicator : GRecorderIndicator
	{
        #region Variables
        private GCDCalculationModeType _calcMode = GCDCalculationModeType.BidAsk;
        private bool _backupMode = true;
        private GFilterModeType _filterMode = GFilterModeType.None;
        private int _filterSize = 1;

        private double _lastPrice = 0;
        private int _lastDirection = 0;
        private bool _startLookingForReversal = false;
        #endregion

        private int _CalcDelta(GTickTypeEnum tickType, double price, int volume, GCDCalculationModeType calcmode, bool backupmode, int filtersize, GFilterModeType filtermode)
        {
            int delta = 0;
            int direction = _lastDirection;

            if ((calcmode == GCDCalculationModeType.BidAsk) && (tickType != GTickTypeEnum.Unknown) && (tickType != GTickTypeEnum.BetweenBidAsk))
            {
                if ((tickType == GTickTypeEnum.BelowBid) || (tickType == GTickTypeEnum.AtBid))
                    delta = -volume;
                else if ((tickType == GTickTypeEnum.AboveAsk) || (tickType == GTickTypeEnum.AtAsk))
                    delta = volume;
            }
            else if (calcmode == GCDCalculationModeType.UpDownTick)
            {
                if (_lastPrice != 0)
                {
                    if (price > _lastPrice) delta = volume;
                    if (price < _lastPrice) delta = -volume;
                }
            }
            else if ((calcmode == GCDCalculationModeType.UpDownTickWithContinuation) || (calcmode == GCDCalculationModeType.UpDownOneTickWithContinuation) || ((calcmode == GCDCalculationModeType.BidAsk) && (backupmode == true)))
            {
                if (price > _lastPrice)  //normal uptick/dn tick
                    direction = 1;
                else if (price < _lastPrice)
                    direction = -1;

                if (calcmode == GCDCalculationModeType.UpDownOneTickWithContinuation)
                    delta = direction;
                else
                    delta = direction * volume;
            }
            // added
            else if ((calcmode == GCDCalculationModeType.Hybrid))
            {
                if (price > _lastPrice)  //normal uptick/dn tick
                {
                    direction = 1;
                    //price changed, we reinit the startlookingforreversal bool.
                    _startLookingForReversal = false;
                }
                else if (price < _lastPrice)
                {
                    direction = -1;
                    _startLookingForReversal = false;
                }

                if (!_startLookingForReversal)
                    if (direction == 1)
                        //if going up, we want to be hitting bid to be able to start to spot reversals (hitting the ask)
                        _startLookingForReversal = (tickType == GTickTypeEnum.AtBid) || (tickType == GTickTypeEnum.BelowBid);
                    else
                        _startLookingForReversal = (tickType == GTickTypeEnum.AtAsk) || (tickType == GTickTypeEnum.AboveAsk);

                //what happens when price is same
                if (price == _lastPrice)
                {
                    //if going up, and we have already hit the bid (startlookingforreversal is true) at a price level, 
                    // and start hitting the ask, let's reverse

                    if ((direction == 1) && _startLookingForReversal && ((tickType == GTickTypeEnum.AtAsk) || (tickType == GTickTypeEnum.BetweenBidAsk)))
                        direction = -1;

                    else if ((direction == -1) && _startLookingForReversal && ((tickType == GTickTypeEnum.AtBid) || (tickType == GTickTypeEnum.BetweenBidAsk)))
                        direction = 1;	//buyers take control of ask
                }

                delta = direction * volume;
            }

            _lastPrice = price;
            _lastDirection = direction;

            if ((filtermode == GFilterModeType.OnlyLargerThan) && (volume <= filtersize))
                delta = 0;

            if ((filtermode == GFilterModeType.OnlySmallerThan) && (volume >= filtersize))
                delta = 0;

            return delta;
        }

        protected int CalcDelta(GMarketDataType e)
        {
            return _CalcDelta(e.tickType, e.price, e.volume, _calcMode, _backupMode, _filterSize, _filterMode);
        }

        protected int CalcDelta(GTickTypeEnum tickType, double price, int volume)
        {
            return _CalcDelta(tickType, price, volume, _calcMode, _backupMode, _filterSize, _filterMode);
        }

        #region Properties
        [Description("UpDownTick : volume is up if price>lastprice, down if price<lastprice.\nUpDownTickWithContinuation : volume is up if price>lastprice or\nprice=lastprice and last direction was up, same for downside")]
        [Category("Parameters")]
        [Gui.Design.DisplayNameAttribute("Delta:Calculation Mode")]
        [Browsable(true)]
        public GCDCalculationModeType CalcMode
        {
            get { return _calcMode; }
            set { _calcMode = value; }
        }

        [Description("Volume Filter")]
        [Category("Parameters")]
        [Gui.Design.DisplayNameAttribute("Delta:Volume Filter Size")]
        [Browsable(true)]
        public int FilterSize
        {
            get { return _filterSize; }
            set { _filterSize = value; }
        }

        [Description("If Bid/ask data invalid, do we use updownwithcontinuation ?")]
        [Category("Parameters")]
        [Gui.Design.DisplayNameAttribute("Delta:Volume UpDownTick Completion")]
        [Browsable(true)]
        public bool BackupMode
        {
            get { return _backupMode; }
            set { _backupMode = value; }
        }

        [Description("Filter Mode")]
        [Category("Parameters")]
        [Gui.Design.DisplayNameAttribute("Delta:Size Filter Mode")]
        [Browsable(true)]
        public GFilterModeType FilterMode
        {
            get { return _filterMode; }
            set { _filterMode = value; }
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
        private GDeltaIndicator[] cacheGDeltaIndicator = null;

        private static GDeltaIndicator checkGDeltaIndicator = new GDeltaIndicator();

        /// <summary>
        /// Base Class. Do not instantiate
        /// </summary>
        /// <returns></returns>
        public GDeltaIndicator GDeltaIndicator(bool backupMode, GCDCalculationModeType calcMode, GFilterModeType filterMode, int filterSize)
        {
            return GDeltaIndicator(Input, backupMode, calcMode, filterMode, filterSize);
        }

        /// <summary>
        /// Base Class. Do not instantiate
        /// </summary>
        /// <returns></returns>
        public GDeltaIndicator GDeltaIndicator(Data.IDataSeries input, bool backupMode, GCDCalculationModeType calcMode, GFilterModeType filterMode, int filterSize)
        {
            if (cacheGDeltaIndicator != null)
                for (int idx = 0; idx < cacheGDeltaIndicator.Length; idx++)
                    if (cacheGDeltaIndicator[idx].BackupMode == backupMode && cacheGDeltaIndicator[idx].CalcMode == calcMode && cacheGDeltaIndicator[idx].FilterMode == filterMode && cacheGDeltaIndicator[idx].FilterSize == filterSize && cacheGDeltaIndicator[idx].EqualsInput(input))
                        return cacheGDeltaIndicator[idx];

            lock (checkGDeltaIndicator)
            {
                checkGDeltaIndicator.BackupMode = backupMode;
                backupMode = checkGDeltaIndicator.BackupMode;
                checkGDeltaIndicator.CalcMode = calcMode;
                calcMode = checkGDeltaIndicator.CalcMode;
                checkGDeltaIndicator.FilterMode = filterMode;
                filterMode = checkGDeltaIndicator.FilterMode;
                checkGDeltaIndicator.FilterSize = filterSize;
                filterSize = checkGDeltaIndicator.FilterSize;

                if (cacheGDeltaIndicator != null)
                    for (int idx = 0; idx < cacheGDeltaIndicator.Length; idx++)
                        if (cacheGDeltaIndicator[idx].BackupMode == backupMode && cacheGDeltaIndicator[idx].CalcMode == calcMode && cacheGDeltaIndicator[idx].FilterMode == filterMode && cacheGDeltaIndicator[idx].FilterSize == filterSize && cacheGDeltaIndicator[idx].EqualsInput(input))
                            return cacheGDeltaIndicator[idx];

                GDeltaIndicator indicator = new GDeltaIndicator();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.BackupMode = backupMode;
                indicator.CalcMode = calcMode;
                indicator.FilterMode = filterMode;
                indicator.FilterSize = filterSize;
                Indicators.Add(indicator);
                indicator.SetUp();

                GDeltaIndicator[] tmp = new GDeltaIndicator[cacheGDeltaIndicator == null ? 1 : cacheGDeltaIndicator.Length + 1];
                if (cacheGDeltaIndicator != null)
                    cacheGDeltaIndicator.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cacheGDeltaIndicator = tmp;
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
        /// Base Class. Do not instantiate
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.GDeltaIndicator GDeltaIndicator(bool backupMode, GCDCalculationModeType calcMode, GFilterModeType filterMode, int filterSize)
        {
            return _indicator.GDeltaIndicator(Input, backupMode, calcMode, filterMode, filterSize);
        }

        /// <summary>
        /// Base Class. Do not instantiate
        /// </summary>
        /// <returns></returns>
        public Indicator.GDeltaIndicator GDeltaIndicator(Data.IDataSeries input, bool backupMode, GCDCalculationModeType calcMode, GFilterModeType filterMode, int filterSize)
        {
            return _indicator.GDeltaIndicator(input, backupMode, calcMode, filterMode, filterSize);
        }
    }
}

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    public partial class Strategy : StrategyBase
    {
        /// <summary>
        /// Base Class. Do not instantiate
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.GDeltaIndicator GDeltaIndicator(bool backupMode, GCDCalculationModeType calcMode, GFilterModeType filterMode, int filterSize)
        {
            return _indicator.GDeltaIndicator(Input, backupMode, calcMode, filterMode, filterSize);
        }

        /// <summary>
        /// Base Class. Do not instantiate
        /// </summary>
        /// <returns></returns>
        public Indicator.GDeltaIndicator GDeltaIndicator(Data.IDataSeries input, bool backupMode, GCDCalculationModeType calcMode, GFilterModeType filterMode, int filterSize)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.GDeltaIndicator(input, backupMode, calcMode, filterMode, filterSize);
        }
    }
}
#endregion
