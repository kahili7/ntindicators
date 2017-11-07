using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NinjaTrader.Indicator
{
    public partial class GRangeProfile : GDeltaIndicator
    {
        #region Variables

        #region Settings
        private string instrument = "";
        #endregion

        private struct RangeProfileType
        {
            public int id;
            public int _Xl;
            public int _Xr;
            public int _Yt;
            public int _Yb;

            public int leftBar;
            public int leftOffset;
            public DateTime leftTime;

            public int rightBar;
            public int rightOffset;
            public DateTime rightTime;

            public Color rangeColor;

            public Dictionary<double, double> vol_price;
            public Dictionary<double, double> work_vol_price;
            public Dictionary<double, double> bars_price;
        }

        private RangeProfileType[] R;

        #region MouseEventHandle
        private MouseEventHandler mouseDownH;
        private MouseEventHandler mouseUpH;
        private MouseEventHandler mouseMoveH;
        private MouseEventHandler mouseDoubleClickH;

        private double min = Double.MaxValue;
        private double max = Double.MinValue;

        delegate int Function(int x);

        private int cursorX = 0;
        private double cursorY = 0;
        #endregion

        #endregion

    }
}
