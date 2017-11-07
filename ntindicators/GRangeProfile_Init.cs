using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;

namespace NinjaTrader.Indicator
{
    public partial class GRangeProfile : GDeltaIndicator
    {
        protected override void GInitialize()
        {
            Overlay = true;
            AutoScale = false;
            CalculateOnBarClose = false;

            Add(PeriodType.Tick, 1);        // ВОТ ключевой ход - мы просто на график любого таймфрейма добавляем еще один - с периодом ОДИН ТИК 
                                            // и таким образом можем КАЖДЫЙ ТИК анализировать и собирать информацию в том числе и по истории!!!
                                            // Только пока не ясно - работает ли данный подход с ДЕЛЬТОЙ (то есть доступна ли нам информация о бидах и асках)???

            Add(new Line(new Pen(Color.Yellow, 3), 0, "VPOCline"));
            Add(new Line(new Pen(Color.Green, 3), 0, "ExtLongVPOC"));
            Add(new Line(new Pen(Color.Red, 3), 0, "ExtShortVPOC"));
            Add(new Line(new Pen(Color.Red, 2), 0, "Borders"));

            Add(new Plot(Color.Transparent, "TEST"));
            BarsRequired = 0;
            ClearOutputWindow();

            instrument = Instrument.MasterInstrument.Name;

        }
    }
}
