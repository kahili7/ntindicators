using System;
using System.ComponentModel;

using NinjaTrader.Data;
using NinjaTrader.Indicator;

namespace NinjaTrader.Data
{
    abstract partial class GFileManager : IGDataManager, IDisposable
    {
        public void Initialize(string instrName, bool writeData, GRecorderIndicator indicator)
        {
            _freeReader();

            tickSize = indicator.BarsArray[0].Instrument.MasterInstrument.TickSize;
            _instrName = instrName;
            _writeData = writeData;

            if (IsWritable)
                _fileMode = indicator.FileMode;
            else
                _fileMode = GFileModeType.SingleFile;

            folderName = indicator.FolderName;
            useMillisec = indicator.UseMillisec;
        }
    }
}

namespace NinjaTrader.Indicator
{
    public partial class GRecorderIndicator : Indicator
    {
        private GFileModeType _fileMode = GFileModeType.OnePerDay;
        public bool useMillisec = true;

        [Description("Recording mode : 1 big file or split per day")]
        [Category("Parameters")]
        [Gui.Design.DisplayName("Rec:Recording Mode")]
        [SpecificTo("Binary", "Flat", "Short", "Millisec")]
        [Browsable(true)]
        public GFileModeType FileMode
        {
            get { return _fileMode; }
            set { _fileMode = value; }
        }
    }
}
