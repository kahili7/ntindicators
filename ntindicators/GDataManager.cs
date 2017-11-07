using System;
using System.Collections.Generic;

namespace NinjaTrader.Data
{
    public enum GTickTypeEnum { BelowBid, AtBid, BetweenBidAsk, AtAsk, AboveAsk, Unknown }
    
    public struct GMarketDataType
    {
        public enum GTimeStampStatus { Same, Different, Unknown }

        public DateTime time;
        public GTickTypeEnum tickType;
        public double price;
        public int volume;
        public GTimeStampStatus isNewTimeStamp;

        public GMarketDataType(DateTime t, GTickTypeEnum tt, double p, int v)
        {
            time = t;
            tickType = tt;
            price = p;
            volume = v;
            isNewTimeStamp = GTimeStampStatus.Unknown;
        }

        public GMarketDataType(DateTime t, GTickTypeEnum tt, double p, int v, GTimeStampStatus b)
        {
            time = t;
            tickType = tt;
            price = p;
            volume = v;
            isNewTimeStamp = b;
        }
    }

    partial interface IGDataManager: IDisposable
    {
        string Name { get; }

        bool IsWritable { get; }

        bool IsMillisecCompliant { get; }

        void SetCursorTime(DateTime dt, ref GMarketDataType data);
        
        void GetNextTick(ref GMarketDataType data);

        bool RecordTick(DateTime dt, double bid, double ask, double price, int volume);
     }

    public static partial class GUtils
    {
        public static DateTime nullDT = new DateTime(0L);

        public static GTickTypeEnum GetIntTickType(double bid, double ask, double price)
        {
            GTickTypeEnum tickType;

            if (ask < bid)
            {
                if (price < ask) tickType = GTickTypeEnum.BelowBid;
                else if (price == ask) tickType = GTickTypeEnum.AtAsk;
                else if (price < bid) tickType = GTickTypeEnum.BetweenBidAsk;
                else if (price == bid) tickType = GTickTypeEnum.AtBid;
                else tickType = GTickTypeEnum.AboveAsk;
            }
            else if (bid < ask)
            {
                if (price < bid) tickType = GTickTypeEnum.BelowBid;
                else if (price == bid) tickType = GTickTypeEnum.AtBid;
                else if (price < ask) tickType = GTickTypeEnum.BetweenBidAsk;
                else if (price == ask) tickType = GTickTypeEnum.AtAsk;
                else tickType = GTickTypeEnum.AboveAsk;
            }
            else //bid==ask
            {
                if (price < bid) tickType = GTickTypeEnum.BelowBid;
                else if (price > ask) tickType = GTickTypeEnum.AboveAsk;
                else tickType = tickType = GTickTypeEnum.BetweenBidAsk;
            }

            return tickType;
        }
    }
}
