using System;
using System.Collections.Generic;

namespace NinjaTrader.Data
{
    #region GBigPrintData
    public class GBigPrintData
    {
        public enum TType
        {
            Unknown = 0,
            Ask = 1,
            Bid = 2,
            Fill = 3
        }

        public struct SDepthData
        {
            #region Variable
            public double BestAsk;
            public double BestBid;

            public int AskVolume;
            public int BidVolume;

            public TType Type;
            public DateTime FillTime;

            private int _priceIndex;
            private int _volumeIndex;
            private ushort[] _volumes;
            private double[] _prices;
            #endregion

            public SDepthData(int count)
            {
                this.BestAsk = 0;
                this.AskVolume = 0;
                this.BestBid = 0;
                this.BidVolume = 0;
                this.FillTime = DateTime.Now;
                this.Type = TType.Unknown;

                this._priceIndex = 0;
                this._volumeIndex = 0;
                this._volumes = new ushort[count];
                this._prices = new double[count];
            }

            public double Price
            {
                get { return _prices[0]; }
            }

            public int PriceCount
            {
                get { return _priceIndex; }
            }

            public int Volume
            {
                get
                {
                    int result = 0;

                    for (int i = 0; i < _volumeIndex; i++)
                        result += _volumes[i];

                    return result;
                }
            }

            public TType FillType
            {
                get
                {
                    if (_priceIndex < 1)
                        return TType.Unknown;

                    double p = _prices[_priceIndex - 1];

                    if (BestAsk == BestBid)
                    {
                        if (p > BestAsk)
                            return TType.Ask;

                        if (p < BestBid)
                            return TType.Bid;

                        return TType.Unknown;
                    }

                    if (p >= BestAsk)
                        return TType.Ask;

                    if (p <= BestBid)
                        return TType.Bid;

                    return TType.Fill; // between ask and bid
                }
            }

            public void GetPrices(out double first, out double last)
            {
                first = _prices[0];
                last = _prices[_priceIndex - 1];
            }

            public void Fill(double price, int volume, DateTime time)
            {
                if (Type != TType.Fill)
                {
                    Type = TType.Fill;
                    FillTime = time;
                    _volumeIndex = 0;
                    _priceIndex = 1;
                    _prices[0] = price;
                }

                _volumes[_volumeIndex++] = (ushort)volume;

                if (_prices[_priceIndex - 1] != price)
                    _prices[_priceIndex++] = price;
            }

            public void UpdateAsk(GBigPrintData data, double price, int volume)
            {
                if (Type == TType.Fill)
                    data.OnFill(this, TType.Ask, price, volume);

                Type = TType.Ask;
                BestAsk = price;
                AskVolume = volume;
            }

            public void UpdateBid(GBigPrintData data, double price, int volume)
            {
                if (Type == TType.Fill)
                    data.OnFill(this, TType.Bid, price, volume);

                Type = TType.Bid;
                BestBid = price;
                BidVolume = volume;
            }
        }

        public interface IFillListener
        {
            void OnFill(double firstFill, double lastFill, int volume, DateTime time);
        }

        #region Variables
        private double _tickSz;
        private double _oPrice;
        private DateTime _expiry;
        private string _instrument;
        private int _digits;

        public IFillListener Listener;
        private SDepthData _depth;

        private List<DateTime> _timeStampList;
        private List<short> _priceList;
        private List<ushort> _volumeList;
        private List<ushort> _secondsDeltaList;
        #endregion

        public GBigPrintData(string instr, DateTime expiryDate, double tick)
		{
			_depth = new SDepthData(1024);
			_tickSz = tick;
			_digits = GetDigits(tick);
			_instrument = instr;
			_expiry = expiryDate;
			
			if(tryToLoad())
				return;
			
			_timeStampList = new List<DateTime>(16);
			_priceList = new List<short>(128);
			_volumeList = new List<ushort>(128);
			_secondsDeltaList = new List<ushort>(128);
		}

        public static int GetDigits(double point)
        {
            int result = 0;
            int pint = (int)point;

            while(point != pint)
            {
                point *= 10.0;
                pint = (int)point;
                result++;
            }

            return result;
        }
        
        private bool tryToLoad()
        {
            return false;
        }

        private void updateTime(DateTime time)
        {
            double secondsDelta = 0;

            if (_timeStampList.Count < 1)
            {
                _timeStampList.Add(time);
                _secondsDeltaList.Add(ushort.MaxValue);
            }
            else
            {
                TimeSpan span = time - _timeStampList[_timeStampList.Count - 1];
                secondsDelta = Math.Round(span.TotalSeconds);

                if (secondsDelta >= ushort.MaxValue)
                {
                    _timeStampList.Add(time);
                    _secondsDeltaList.Add(ushort.MaxValue);
                }
                else
                {
                    _secondsDeltaList.Add((ushort)secondsDelta);
                }
            }
        }

        private short getOpenPriceDelta(double price)
        {
            return (short)(price / _tickSz - _oPrice / _tickSz);
        }

        private double getPriceFromDelta(short delta)
        {
            return Math.Round((_oPrice / _tickSz + delta) * _tickSz, _digits);
        }

        public void UpdateAsk(double price, long volume)
        {
            _depth.UpdateAsk(this, price, (int)volume);
        }

        public void UpdateBid(double price, long volume)
        {
            _depth.UpdateBid(this, price, (int)volume);
        }

        public void Fill(double price, long volume, DateTime time)
        {
            _depth.Fill(price, (int)volume, time);
        }

        internal void OnFill(SDepthData data, TType priceType, double newPrice, int newVolume)
        {
            int volume = data.Volume;
            TType fillType = data.FillType;

            if (fillType == TType.Unknown || fillType == TType.Fill)
                fillType = priceType;

            if (fillType == TType.Bid)
                volume = -volume;

            if (_oPrice == 0.0)
                _oPrice = data.Price;

            if (Listener == null)
                return;

            double firstPrice = 0;
            double lastPrice = 0;

            data.GetPrices(out firstPrice, out lastPrice);
            Listener.OnFill(firstPrice, lastPrice, volume, data.FillTime);
        }

        #region Properties
        public double TickSize
        { 
            get { return _tickSz; }
        }
        #endregion
    }
    #endregion
}