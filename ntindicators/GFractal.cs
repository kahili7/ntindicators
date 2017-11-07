#region Using declarations
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.Xml.Serialization;

using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
#endregion

namespace NinjaTrader.Indicator
{
    public class GFractal : Indicator
    {
        #region Variables
        private int _barsFractal = 2;

        private double _highVal = 0;
        private double _lowVal = 0;

        private DataSeries _fractalHigh;
        private DataSeries _fractalLow;

        private bool _showPattern = true;
        #endregion

        protected override void Initialize()
        {
            Add(new Plot(Color.Red, PlotStyle.Dot, "Fractal high"));
            Add(new Plot(Color.Green, PlotStyle.Dot, "Fractal low"));
            Plots[0].Pen.DashStyle = DashStyle.Dot;
            Plots[1].Pen.DashStyle = DashStyle.Dot;

            _fractalHigh = new DataSeries(this);
            _fractalLow = new DataSeries(this);

            BarsRequired = 0;
            DisplayInDataBox = false;
            PaintPriceMarkers = false;
            Overlay = true;
        }

        protected override void OnStartUp()
        {
            if (ChartControl != null)
            {
                Plots[0].Pen.Color = ChartControl.ChartStyle.DownColor;
                Plots[1].Pen.Color = ChartControl.ChartStyle.UpColor;

                switch (ChartControl.ChartStyle.ChartStyleType)
                {
                    case ChartStyleType.CandleStick:
                        Plots[0].Pen.Width = ChartControl.ChartStyle.Pen2.Width;
                        Plots[1].Pen.Width = ChartControl.ChartStyle.Pen2.Width;
                        break;

                    case ChartStyleType.Box:
                        Plots[0].Pen.Width = Math.Max(ChartControl.ChartStyle.Pen.Width, ChartControl.ChartStyle.Pen2.Width);
                        Plots[1].Pen.Width = Math.Max(ChartControl.ChartStyle.Pen.Width, ChartControl.ChartStyle.Pen2.Width);
                        break;

                    default:
                        Plots[0].Pen.Width = ChartControl.ChartStyle.Pen.Width;
                        Plots[1].Pen.Width = ChartControl.ChartStyle.Pen.Width;
                        break;
                }
            }
        }

        protected override void OnBarUpdate()
        {
            try
            {
                isHighPivot(Period);
                isLowPivot(Period);

                #region GPattern
                if (CurrentBar > 20 && ShowPattern)
                {
                    double top0, bot0;
                    double top1, bot1;

                    if (Open[0] > Close[0])
                    {
                        top0 = Open[0];
                        bot0 = Close[0];
                    }
                    else if (Open[0] < Close[0])
                    {
                        top0 = Close[0];
                        bot0 = Open[0];
                    }
                    else
                    {
                        top0 = bot0 = Open[0];
                    }

                    if (Open[1] > Close[1])
                    {
                        top1 = Open[1];
                        bot1 = Close[1];
                    }
                    else if (Open[1] < Close[1])
                    {
                        top1 = Close[1];
                        bot1 = Open[1];
                    }
                    else
                    {
                        top1 = bot1 = Open[1];
                    }


                    // Long
                    if (Low[0] <= Low[1] && (top0 >= High[1] || top0 > top1) && High[1] > top1 && High[0] >= top0 && Low[0] < bot0 && Low[1] <= bot1 && bot0 >= bot1)
                    {
                        DrawText("Long" + CurrentBar, false, "Long", 0, Low[0], -10, Color.Green, new Font("Arial", 6, FontStyle.Bold), StringAlignment.Center, Color.Transparent, Color.Transparent, 0);
                    }

                    // Short
                    if (High[0] >= High[1] && (bot0 <= Low[1] || bot0 < bot1) && Low[1] < bot1 && Low[0] <= bot0 && High[0] > top0 && High[1] >= top1 && top0 <= top1)
                    {
                        DrawText("Short" + CurrentBar, false, "Short", 0, High[0], 10, Color.Red, new Font("Arial", 6, FontStyle.Bold), StringAlignment.Center, Color.Transparent, Color.Transparent, 0);
                    }
                }
                #endregion
            }
            catch (Exception ex)
            { }
        }

        #region Private functions
        private void isHighPivot(int period)
        {
            int y = 0;
            int Lvls = 0;

            //4 Highs
            if (High[period] == High[period + 1] && High[period] == High[period + 2] && High[period] == High[period + 3])
            {
                y = 1;

                while (y <= period)
                {
                    if (y != period ? High[period + 3] > High[period + 3 + y] : High[period + 3] > High[period + 3 + y])
                        Lvls++;

                    if (y != period ? High[period] > High[period - y] : High[period] > High[period - y])
                        Lvls++;

                    y++;
                }
            }
            //3 Highs 
            else if (High[period] == High[period + 1] && High[period] == High[period + 2])
            {
                y = 1;

                while (y <= period)
                {
                    if (y != period ? High[period + 2] > High[period + 2 + y] : High[period + 2] > High[period + 2 + y])
                        Lvls++;

                    if (y != period ? High[period] > High[period - y] : High[period] > High[period - y])
                        Lvls++;

                    y++;
                }
            }
            //2 Highs
            else if (High[period] == High[period + 1])
            {
                y = 1;

                while (y <= period)
                {
                    if (y != period ? High[period + 1] > High[period + 1 + y] : High[period + 1] > High[period + 1 + y])
                        Lvls++;

                    if (y != period ? High[period] > High[period - y] : High[period] > High[period - y])
                        Lvls++;

                    y++;
                }
            }
            else
            {
                y = 1;

                while (y <= period)
                {
                    if (y != period ? High[period] > High[period + y] : High[period] > High[period + y])
                        Lvls++;

                    if (y != period ? High[period] > High[period - y] : High[period] > High[period - y])
                        Lvls++;

                    y++;
                }
            }

            //other checks
            if (Lvls < period * 2)
            {
                Lvls = 0;

                //Four Highs - First and Last Matching - Middle 2 are lower
                if (High[period] >= High[period + 1] && High[period] >= High[period + 2] && High[period] == High[period + 3])
                {
                    y = 1;

                    while (y <= period)
                    {
                        if (y != period ? High[period + 3] > High[period + 3 + y] : High[period + 3] > High[period + 3 + y])
                            Lvls++;

                        if (y != period ? High[period] > High[period - y] : High[period] > High[period - y])
                            Lvls++;

                        y++;
                    }
                }
            }

            if (Lvls < period * 2)
            {
                Lvls = 0;

                //Three Highs - Middle is lower than two outside
                if (High[period] >= High[period + 1] && High[period] == High[period + 2])
                {
                    y = 1;

                    while (y <= period)
                    {
                        if (y != period ? High[period + 2] > High[period + 2 + y] : High[period + 2] > High[period + 2 + y])
                            Lvls++;

                        if (y != period ? High[period] > High[period - y] : High[period] > High[period - y])
                            Lvls++;

                        y++;
                    }
                }
            }

            if (Lvls >= period * 2)
            {
                _highVal = High[period] + TickSize;
                PlotHigh.Set(period, _highVal);
                _fractalHigh.Set(period, _highVal);
            }
        }

        private void isLowPivot(int period)
        {
            int y = 0;
            int Lvls = 0;

            //4 Highs
            if (Low[period] == Low[period + 1] && Low[period] == Low[period + 2] && Low[period] == Low[period + 3])
            {
                y = 1;

                while (y <= period)
                {
                    if (y != period ? Low[period + 3] < Low[period + 3 + y] : Low[period + 3] < Low[period + 3 + y])
                        Lvls++;

                    if (y != period ? Low[period] < Low[period - y] : Low[period] < Low[period - y])
                        Lvls++;

                    y++;
                }
            }
            //3 Highs
            else if (Low[period] == Low[period + 1] && Low[period] == Low[period + 2])
            {
                y = 1;

                while (y <= period)
                {
                    if (y != period ? Low[period + 2] < Low[period + 2 + y] : Low[period + 2] < Low[period + 2 + y])
                        Lvls++;

                    if (y != period ? Low[period] < Low[period - y] : Low[period] < Low[period - y])
                        Lvls++;

                    y++;
                }
            }
            //2 Highs
            else if (Low[period] == Low[period + 1])
            {
                y = 1;

                while (y <= period)
                {
                    if (y != period ? Low[period + 1] < Low[period + 1 + y] : Low[period + 1] < Low[period + 1 + y])
                        Lvls++;

                    if (y != period ? Low[period] < Low[period - y] : Low[period] < Low[period - y])
                        Lvls++;

                    y++;
                }
            }
            else
            {
                y = 1;

                while (y <= period)
                {
                    if (y != period ? Low[period] < Low[period + y] : Low[period] < Low[period + y])
                        Lvls++;

                    if (y != period ? Low[period] < Low[period - y] : Low[period] < Low[period - y])
                        Lvls++;

                    y++;
                }
            }

            //other checks
            if (Lvls < period * 2)
            {
                Lvls = 0;

                //Four Lows - First and Last Matching - Middle 2 are lower
                if (Low[period] <= Low[period + 1] && Low[period] <= Low[period + 2] && Low[period] == Low[period + 3])
                {
                    y = 1;

                    while (y <= period)
                    {
                        if (y != period ? Low[period + 3] < Low[period + 3 + y] : Low[period + 3] < Low[period + 3 + y])
                            Lvls++;

                        if (y != period ? Low[period] < Low[period - y] : Low[period] < Low[period - y])
                            Lvls++;

                        y++;
                    }
                }
            }

            if (Lvls < period * 2)
            {
                Lvls = 0;

                //Three Lows - Middle is lower than two outside
                if (Low[period] <= Low[period + 1] && Low[period] == Low[period + 2])
                {
                    y = 1;

                    while (y <= period)
                    {
                        if (y != period ? Low[period + 2] < Low[period + 2 + y] : Low[period + 2] < Low[period + 2 + y])
                            Lvls++;

                        if (y != period ? Low[period] < Low[period - y] : Low[period] < Low[period - y])
                            Lvls++;

                        y++;
                    }
                }
            }

            if (Lvls >= period * 2)
            {
                _lowVal = Low[period] - TickSize;
                PlotLow.Set(period, _lowVal);
                _fractalLow.Set(period, _lowVal);
            }
        }
        #endregion

        public int FractalHighBar(int barsAgo, int instance, int lookBackPeriod)
        {
            if (instance < 1)
                throw new Exception(GetType().Name + ".FractalHighBar: instance must be greater/equal 1 but was " + instance);
            else if (barsAgo < 0)
                throw new Exception(GetType().Name + ".FractalHighBar: barsAgo must be greater/equal 0 but was " + barsAgo);
            else if (barsAgo >= Count)
                throw new Exception(GetType().Name + ".FractalHighBar: barsAgo out of valid range 0 through " + (Count - 1) + ", was " + barsAgo + ".");

            Update();

            for (int idx = CurrentBar - barsAgo; idx >= CurrentBar - barsAgo - lookBackPeriod; idx--)
            {
                if (idx < 0)
                    return -1;
                if (idx >= _fractalHigh.Count)
                    continue;

                if (_fractalHigh.Get(idx).Equals(0.0))
                    continue;

                if (instance <= 1) // 1-based, < to be save
                    return CurrentBar - idx;

                instance--;
            }


            return -1;
        }

        public int FractalLowBar(int barsAgo, int instance, int lookBackPeriod)
        {
            if (instance < 1)
                throw new Exception(GetType().Name + ".FractalLowBar: instance must be greater/equal 1 but was " + instance);
            else if (barsAgo < 0)
                throw new Exception(GetType().Name + ".FractalLowBar: barsAgo must be greater/equal 0 but was " + barsAgo);
            else if (barsAgo >= Count)
                throw new Exception(GetType().Name + ".FractalLowBar: barsAgo out of valid range 0 through " + (Count - 1) + ", was " + barsAgo + ".");

            Update();

            for (int idx = CurrentBar - barsAgo; idx >= CurrentBar - barsAgo - lookBackPeriod; idx--)
            {
                if (idx < 0)
                    return -1;
                if (idx >= _fractalLow.Count)
                    continue;

                if (_fractalLow.Get(idx).Equals(0.0))
                    continue;

                if (instance == 1) // 1-based, < to be save
                    return CurrentBar - idx;

                instance--;
            }

            return -1;
        }

        #region Properties
        [Description("Period")]
        [Category("Parameters")]
        [Gui.Design.DisplayName("Period")]
        public int Period
        {
            get { return _barsFractal; }
            set { _barsFractal = Math.Max(1, value); }
        }

        [Description("Show pattern")]
        [Category("Parameters")]
        [Gui.Design.DisplayName("ShowPattern")]
        public bool ShowPattern
        {
            get { return _showPattern; }
            set { _showPattern = value; }
        }

        private DataSeries PlotHigh
        {
            get
            {
                Update();
                return Values[0];
            }
        }

        private DataSeries PlotLow
        {
            get
            {
                Update();
                return Values[1];
            }
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
        private GFractal[] cacheGFractal = null;

        private static GFractal checkGFractal = new GFractal();

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public GFractal GFractal(int size, bool showp)
        {
            return GFractal(Input, size , showp);
        }

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public GFractal GFractal(Data.IDataSeries input, int size, bool showp)
        {
            if (cacheGFractal != null)
                for (int idx = 0; idx < cacheGFractal.Length; idx++)
                    if (cacheGFractal[idx].Period == size && cacheGFractal[idx].ShowPattern == showp && cacheGFractal[idx].EqualsInput(input))
                        return cacheGFractal[idx];

            lock (checkGFractal)
            {
                checkGFractal.Period = size;
                size = checkGFractal.Period;

                checkGFractal.ShowPattern = showp;
                showp = checkGFractal.ShowPattern;

                if (cacheGFractal != null)
                    for (int idx = 0; idx < cacheGFractal.Length; idx++)
                        if (cacheGFractal[idx].Period == size && cacheGFractal[idx].ShowPattern == showp && cacheGFractal[idx].EqualsInput(input))
                            return cacheGFractal[idx];

                GFractal indicator = new GFractal();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.Period = size;
                indicator.ShowPattern = showp;
                Indicators.Add(indicator);
                indicator.SetUp();

                GFractal[] tmp = new GFractal[cacheGFractal == null ? 1 : cacheGFractal.Length + 1];
                if (cacheGFractal != null)
                    cacheGFractal.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cacheGFractal = tmp;
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
        public Indicator.GFractal GFractal(int size, bool showp)
        {
            return _indicator.GFractal(Input, size, showp);
        }

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public Indicator.GFractal GFractal(Data.IDataSeries input, int size, bool showp)
        {
            return _indicator.GFractal(input, size, showp);
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
        public Indicator.GFractal GFractal(int size, bool showp)
        {
            return _indicator.GFractal(Input, size, showp);
        }

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public Indicator.GFractal GFractal(Data.IDataSeries input, int size, bool showp)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.GFractal(input, size, showp);
        }
    }
}
#endregion