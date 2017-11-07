#region Using declarations
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using System.Windows.Forms;
using System.Linq;

using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;

using ManagedCuda;
using ManagedCuda.BasicTypes;

using nt.math;
using nt.math.statistics;
using nt.math.decompositions;
#endregion

// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    using System.Collections.Generic;
    using nt.math;

    [Description("WM")]
    public class GWM : Indicator
    {
        public struct CorrRes
        {
            public int i;
            public int j_max;
            public int j_min;
            public double max;
            public double min;
        }

        #region Variables
        private bool _debug = true;

        private Button  _wmButton = null;
        private Button  _wmButton90 = null;
        private bool    _calc_wm;
        private bool    _calc_wm90;    

        #region Calc Matrix 0
        private GWMFunction         _wm;        // вычисление функции
        private GWMResult           _wmRes;     // получение результата

        private int     _cntItem;   // количество элементов в партии
        private int     _cntPoint;  // количество точек в функции

        private int[]       _x;         // значения по оси X
        private DateTime[]  _xTime;     // значения по оси X
        private double[]    _yPrice;    // цена по оси Y

        private double[]    _B;     // вектор коэффициентов B
        private double[]    _D;     // вектор коэффициентов D
        private double[]    _A;     // матрица A

        private double[][,] _yDict;
        private double[][] _resDict;

        private List<CorrRes> _corr;
        #endregion

        #region Calc Matrix 90
        private GWMFunction _wm90;        // вычисление функции
        private GWMResult _wmRes90;     // получение результата

        private int _cntItem90;   // количество элементов в партии
        private int _cntPoint90;  // количество точек в функции

        private int[] _x90;         // значения по оси X
        private DateTime[] _xTime90;     // значения по оси X
        private double[] _yPrice90;    // цена по оси Y

        private double[] _B90;     // вектор коэффициентов B
        private double[] _D90;     // вектор коэффициентов D
        private double[] _A90;     // матрица A

        private double[][,] _yDict90;
        private double[][] _resDict90;

        private List<CorrRes> _corr90;
        #endregion

        // Wizard generated variables
        private int size = 1; // Default setting for Size
        // User defined variables (add any user defined variables below)
        #endregion

        #region Private functions
        private void _PrintArray(double[] m)
        {
            string s = "";

            for (int i = 0; i < m.GetLength(0); i++)
                s += i + " => " + m[i] + "\r\n";

            Print(s);
        }

        private void _PrintMatrix(double[,] m)
        {
            string s = "";

            for (int i = 0; i < m.GetLength(0); i++)
            {
                for (int j = 0; j < m.GetLength(1); j++)
                    s += i + " => " + m[i, j] + " ";

                s += "\r\n";
            }

            Print(s);
        }

        private void _PrintMatrix(double[][] m)
        {
            string s = "";

            for (int i = 0; i < m.GetLength(0); i++)
            {
                for (int j = 0; j < m[i].GetLength(0); j++)
                    s += i + " => " + m[i][j] + " ";

                s += "\r\n";
            }

            Print(s);
        }

        private void _InitWM(int cnt)
        {
            _cntItem = cnt;
            _cntPoint = 5000;

            _x = new int[_cntItem];
            _yPrice = new double[_cntItem];
            _xTime = new DateTime[_cntItem];
            _A = new double[_cntItem];
            _B = new double[1];
            _D = new double[1];

            _yDict = new double[_cntPoint - _cntItem][,];
            _resDict = new double[_cntPoint - _cntItem][];

            for (int i = 0; i < _cntItem; i++)
            {
                _x[i] = i;
            }

            _B[0] = 1.6;
            _D[0] = 1.475;

            if (_debug) Print("Определение переменных _InitWM");
        }

        private void _InitWM90(int cnt)
        {
            _cntItem90 = cnt;
            _cntPoint90 = 5000;

            _x90 = new int[_cntItem90];
            _yPrice90 = new double[_cntItem90];
            _xTime90 = new DateTime[_cntItem90];
            _A90 = new double[_cntItem90];
            _B90 = new double[1];
            _D90 = new double[1];

            _yDict90 = new double[_cntPoint90 - _cntItem90][,];
            _resDict90 = new double[_cntPoint90 - _cntItem90][];

            for (int i = 0; i < _cntItem90; i++)
            {
                _x90[i] = i;
            }

            _B90[0] = 1.6;
            _D90[0] = 1.475;

            if(_debug) Print("Определение переменных _InitWM90");
        }

        // вычисление  
        private void _CalcWM()
        {
            _wmRes = (new GWMFunction(-30, 30, _B[0], _D[0], _cntPoint, 0.0)).WMFuncResult();
            
            // разбиение графика
            for (int i = 0; i < (_cntPoint - _cntItem); i++)
            {
                _yDict[i] = new double[_cntItem, 2];

                var j = 0;

                for (int k = i; k < (i + _cntItem); k++)
                {
                    _yDict[i][j, 0] = _wmRes.Y[k];
                    _yDict[i][j, 1] = 1;
                    j++;
                }
            }

            if (_debug) Print("Разбиение графика _CalcWM");

            // подсчет результатов
            for (int i = 0; i < (_cntPoint - _cntItem); i++)
            {
                SingularValueDecomposition svd = new SingularValueDecomposition(_yDict[i], true, true);
                double[] x = svd.Solve(_A);
                _resDict[i] = new double[_cntItem];

                for (int k = 0; k < _cntItem; k++)
                {
                    _resDict[i][k] = _yDict[i][k, 0] * x[0] + _yDict[i][k, 1] * x[1];
                }
            }

            if (_debug) Print("Подсчет результатов _CalcWM");
        }

        private void _CalcWM90()
        {
            _wmRes90 = (new GWMFunction(-30, 30, _B90[0], _D90[0], _cntPoint90, 1.5707963267948966192313216916398)).WMFuncResult();

            // разбиение графика
            for (int i = 0; i < (_cntPoint90 - _cntItem90); i++)
            {
                _yDict90[i] = new double[_cntItem90, 2];

                var j = 0;

                for (int k = i; k < (i + _cntItem90); k++)
                {
                    _yDict90[i][j, 0] = _wmRes90.Y[k];
                    _yDict90[i][j, 1] = 1;
                    j++;
                }
            }

            if (_debug) Print("Разбиение графика _CalcWM90");

            // подсчет результатов
            for (int i = 0; i < (_cntPoint90 - _cntItem90); i++)
            {
                SingularValueDecomposition svd = new SingularValueDecomposition(_yDict90[i], true, true);
                double[] x = svd.Solve(_A90);
                _resDict90[i] = new double[_cntItem90];

                for (int k = 0; k < _cntItem90; k++)
                {
                    _resDict90[i][k] = _yDict90[i][k, 0] * x[0] + _yDict90[i][k, 1] * x[1];
                }
            }

            if (_debug) Print("Подсчет результатов _CalcWM90");
        }

        private void _CalcCorr()
        {
            _corr = new List<CorrRes>(_cntPoint - _cntItem);

            for (int i = 0; i < (_cntPoint - _cntItem); i++)
            {
                CorrRes tmp = new CorrRes();
                double[] res = new double[(_cntPoint - _cntItem)];

                tmp.i = i;

                for(int j = 0; j < (_cntPoint - _cntItem); j++)
                {
                    res[j] = Covariance.Correlation(_A, _resDict[j]);            
                }

                tmp.max = res.Max();
                tmp.j_max = Array.IndexOf(res, tmp.max);
                tmp.min = res.Min();
                tmp.j_min = Array.IndexOf(res, tmp.min);
                _corr.Add(tmp);
            }

            int[] arr_i = (from el in _corr orderby el.max select el.i).ToArray<int>();
            int[] arr_j_max = (from el in _corr orderby el.max select el.j_max).ToArray<int>();
            int j_max = arr_j_max.Last();

            Print("j_max = " + j_max);
            _DrawGraphArray(_resDict[j_max], _xTime, _resDict[j_max + _cntItem - 1]);
        }

        private void _CalcCorr90()
        {
            _corr90 = new List<CorrRes>(_cntPoint90 - _cntItem90);

            for (int i = 0; i < (_cntPoint90 - _cntItem90); i++)
            {
                CorrRes tmp = new CorrRes();
                double[] res = new double[(_cntPoint90 - _cntItem90)];

                tmp.i = i;

                for (int j = 0; j < (_cntPoint90 - _cntItem90); j++)
                {
                    res[j] = Covariance.Correlation(_A90, _resDict90[j]);
                }

                tmp.max = res.Max();
                tmp.j_max = Array.IndexOf(res, tmp.max);
                tmp.min = res.Min();
                tmp.j_min = Array.IndexOf(res, tmp.min);
                _corr90.Add(tmp);
            }

            int[] arr_i = (from el in _corr90 orderby el.max select el.i).ToArray<int>();
            int[] arr_j_max = (from el in _corr90 orderby el.max select el.j_max).ToArray<int>();
            int j_max = arr_j_max.Last();

            Print("(90) j_max = " + j_max);
            _DrawGraphArray90(_resDict90[j_max], _xTime90, _resDict90[j_max + _cntItem90 - 1]);
        }

        private void _ToggleWM(object sender, EventArgs e)
        {
            if (!_calc_wm)
            {
                _wmButton.BackColor = Color.LightSalmon;
                _calc_wm = true;            
            }
            else if (_calc_wm)
            {              
                _CalcWM();
                _CalcCorr();
                _wmButton.BackColor = Color.GreenYellow;
                _calc_wm = false;
            }

            this.ChartControl.ChartPanel.Invalidate();
        }

        private void _ToggleWM90(object sender, EventArgs e)
        {
            if (!_calc_wm90)
            {
                _wmButton90.BackColor = Color.LightSalmon;
                _calc_wm90 = true;
            }
            else if (_calc_wm90)
            {
                _CalcWM90();
                _CalcCorr90();
                _wmButton90.BackColor = Color.GreenYellow;
                _calc_wm90 = false;
            }

            this.ChartControl.ChartPanel.Invalidate();
        }

        private void _DrawGraphArray(double[] arr, DateTime[] arr_t, double[] arr_next)
        {
            double delta = 5.0;
            double min = arr.Min();
            int addMin = 0;
            int tmpMin = BarsPeriod.Value;

            if (arr.Length == arr_next.Length)
            {
                for (int i = 1; i < arr.Length; i++)
                {
                    DrawLine("PointWM" + arr_t[i - 1], true, arr_t[i - 1], arr[i - 1] + delta, arr_t[i], arr[i] + delta, Color.Blue, DashStyle.Solid, 2);
                    //DrawLine("PointNextWM" + arr_t[i - 1], true, arr_t[i - 1], arr_next[i - 1] + 2*delta, arr_t[i], arr_next[i] + 2*delta, Color.Magenta, DashStyle.Solid, 2);
                }

                addMin = 0;

                for (int i = 1; i < arr_next.Length; i++)
                {
                    DrawLine("PointNextWM" + arr_t[i - 1], true, arr_t[arr_next.Length - 1].AddMinutes(addMin), arr_next[i - 1], arr_t[arr_next.Length - 1].AddMinutes(addMin + tmpMin), arr_next[i], Color.Green, DashStyle.Solid, 2);
                    addMin += tmpMin;
                }
            }

            /*for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] > min) arr[i] = min - (arr[i] - min);
                else if (arr[i] < min) arr[i] = min + (arr[i] + min);
            }

            // инверсия графика
            for (int i = 1; i < arr.Length; i++)
            {
                DrawLine("InPointWM" + arr_t[i - 1], true, arr_t[i - 1], arr[i - 1] + 2.5 * delta, arr_t[i], arr[i] + 2.5 * delta, Color.Red, DashStyle.Solid, 2);
            }*/
        }

        private void _DrawGraphArray90(double[] arr, DateTime[] arr_t, double[] arr_next)
        {
            double delta = 5.0;
            double min = arr.Min();
            int addMin = 0;
            int tmpMin = BarsPeriod.Value;

            if (arr.Length == arr_next.Length)
            {
                for (int i = 1; i < arr.Length; i++)
                {
                    DrawLine("PointWM90" + arr_t[i - 1], true, arr_t[i - 1], arr[i - 1] + delta, arr_t[i], arr[i] + delta, Color.LightBlue, DashStyle.Solid, 2);
                    //DrawLine("PointNextWM" + arr_t[i - 1], true, arr_t[i - 1], arr_next[i - 1] + 2*delta, arr_t[i], arr_next[i] + 2*delta, Color.Magenta, DashStyle.Solid, 2);
                }

                addMin = 0;

                for (int i = 1; i < arr_next.Length; i++)
                {
                    DrawLine("PointNextWM90" + arr_t[i - 1], true, arr_t[arr_next.Length - 1].AddMinutes(addMin), arr_next[i - 1], arr_t[arr_next.Length - 1].AddMinutes(addMin + tmpMin), arr_next[i], Color.Gold, DashStyle.Solid, 2);
                    addMin += tmpMin;
                }
            }

            for (int i = 0; i < arr.Length; i++)
            {
                if (arr_next[i] > min) arr_next[i] = min - (arr_next[i] - min);
                else if (arr_next[i] < min) arr_next[i] = min + (arr_next[i] + min);
            }

            // инверсия графика
            addMin = 0;

            for (int i = 1; i < arr_next.Length; i++)
            {
                DrawLine("InPointNextWM90" + arr_t[i - 1], true, arr_t[arr_next.Length - 1].AddMinutes(addMin), arr_next[i - 1], arr_t[arr_next.Length - 1].AddMinutes(addMin + tmpMin), arr_next[i], Color.LightGreen, DashStyle.Solid, 2);
                addMin += tmpMin;
            }
        }
        #endregion

        protected override void Initialize()
        {            
            Overlay	= true;
        }

        protected override void OnStartUp()
        {
            _calc_wm = false;
            _calc_wm90 = false;
            _InitWM(CountItem);
            _InitWM90(CountItem);

            // Фаза 0
            _wmButton = new Button();
            _wmButton.Location = new Point(this.ChartControl.CanvasLeft + 70, this.ChartControl.CanvasTop + 15);
            _wmButton.Size = new Size(60, 27);
            _wmButton.BackColor = Color.LightSalmon;
            _wmButton.Name = "wmButton";
            _wmButton.Text = "Фаза-0";
            this.ChartControl.ChartPanel.Controls.Add(_wmButton);
            _wmButton.Click += _ToggleWM;

            // Фаза 90
            _wmButton90 = new Button();
            _wmButton90.Location = new Point(this.ChartControl.CanvasLeft + 140, this.ChartControl.CanvasTop + 15);
            _wmButton90.Size = new Size(70, 27);
            _wmButton90.BackColor = Color.LightSalmon;
            _wmButton90.Name = "wmButton90";
            _wmButton90.Text = "Фаза-90";
            this.ChartControl.ChartPanel.Controls.Add(_wmButton90);
            _wmButton90.Click += _ToggleWM90;
        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {

            if (CurrentBar < CountItem + 50) return;
            
            int k = CountItem - 1;

            // сохранение значение цен на интервале
            if (!_calc_wm || !_calc_wm90)
            {
                for (int i = 0; i < CountItem; i++)
                {
                    _yPrice[k] = _A[k] = _yPrice90[k] = _A90[k] = Close[i];
                    _xTime[k] = _xTime90[k] = Time[i];
                    k--;
                }
            }
        }

        protected override void OnTermination()
        {
            if(_wmButton != null)
            {
                _wmButton.Click -= _ToggleWM;
                this.ChartControl.ChartPanel.Controls.Remove(_wmButton);
                _wmButton.Dispose();
            }

            if (_wmButton90 != null)
            {
                _wmButton90.Click -= _ToggleWM90;
                this.ChartControl.ChartPanel.Controls.Remove(_wmButton90);
                _wmButton90.Dispose();
            }
        }

        #region Properties

        [Description("")]
        [GridCategory("Parameters")]
        public int Size
        {
            get { return size; }
            set { size = Math.Max(1, value); }
        }

        [Description("Count value")]
        [GridCategory("Parameters")]
        public int CountItem
        {
            get { return _cntItem; }
            set { _cntItem = Math.Max(10, value); }
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
        private GWM[] cacheGWM = null;

        private static GWM checkGWM = new GWM();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public GWM GWM(int size)
        {
            return GWM(Input, size);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public GWM GWM(Data.IDataSeries input, int size)
        {
            if (cacheGWM != null)
                for (int idx = 0; idx < cacheGWM.Length; idx++)
                    if (cacheGWM[idx].Size == size && cacheGWM[idx].EqualsInput(input))
                        return cacheGWM[idx];

            lock (checkGWM)
            {
                checkGWM.Size = size;
                size = checkGWM.Size;

                if (cacheGWM != null)
                    for (int idx = 0; idx < cacheGWM.Length; idx++)
                        if (cacheGWM[idx].Size == size && cacheGWM[idx].EqualsInput(input))
                            return cacheGWM[idx];

                GWM indicator = new GWM();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.Size = size;
                Indicators.Add(indicator);
                indicator.SetUp();

                GWM[] tmp = new GWM[cacheGWM == null ? 1 : cacheGWM.Length + 1];
                if (cacheGWM != null)
                    cacheGWM.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cacheGWM = tmp;
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
        /// 
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.GWM GWM(int size)
        {
            return _indicator.GWM(Input, size);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Indicator.GWM GWM(Data.IDataSeries input, int size)
        {
            return _indicator.GWM(input, size);
        }
    }
}

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    public partial class Strategy : StrategyBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.GWM GWM(int size)
        {
            return _indicator.GWM(Input, size);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Indicator.GWM GWM(Data.IDataSeries input, int size)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.GWM(input, size);
        }
    }
}
#endregion
