using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace NinjaTrader.Data
{
    using NinjaTrader.Indicator;

    partial interface IGDataManager : IDisposable
    {
        void Initialize(string instrName, bool writeData, GRecorderIndicator indicator);
    }

    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class SpecificTo : System.Attribute
    {
        public string[] Name;

        public SpecificTo(params string[] param)
        {
            Name = param;
        }

        public SpecificTo(string param)
        {
            Name = new string[] { param };
        }
    }

    static class GDataManagerList
    {
        public static List<string> Name = new List<string>();
        public static List<bool> Writable = new List<bool>();
        public static List<bool> MillisecCompliant = new List<bool>();
        public static List<Type> Type = new List<Type>();

        static GDataManagerList()
        {
            var types = from t in Assembly.GetExecutingAssembly().GetTypes()
                        where t.IsClass && !t.IsAbstract && (t.GetInterface(typeof(IGDataManager).Name) != null)
                        select t;

            foreach (var type in types)
            {
                IGDataManager instance = (IGDataManager)Activator.CreateInstance(type);

                Name.Add((string)(type.GetProperty("Name").GetValue(instance, null)));
                Writable.Add((bool)(type.GetProperty("IsWritable").GetValue(instance, null)));
                MillisecCompliant.Add((bool)(type.GetProperty("IsMillisecCompliant").GetValue(instance, null)));
                Type.Add(type);

                instance.Dispose();
            }
        }
    }

    public class GDataManagerConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            StandardValuesCollection cols = new StandardValuesCollection(GDataManagerList.Name);
            return cols;
        }
    }  
}
