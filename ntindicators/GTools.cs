#region Using declarations
using System;
//using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
//using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
//using System.Xml.Serialization;
using System.Windows.Forms;

//using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
//using NinjaTrader.Gui.Design;
#endregion

#region Enums
public enum AutoScrollButtonPosition
{
    TOP_LEFT,
    TOP_RIGHT
}

public enum AutoScrollType
{
    Center,
    Extreem
}

public enum ObjectPosition
{ 
    BottomRight, 
    BottomLeft,
    TopRight, 
    TopLeft, 
    Centre,
    TopCentre,
    BottomCentre
}

public enum DisplayType
{
    All, 
    LapsedTimeOnly, 
    ClockOnly
}
#endregion

namespace NinjaTrader.Indicator
{
    [Description("Enter the description of your new custom indicator here")]
    public class GTools : GDeltaIndicator, GBigPrintData.IFillListener
    {
        #region Variables
        private string _fnameINI;

            #region VScrollBar
                private VScrollBar _vsbar = null;
                private Button _asbutton = null;
                private Button _vdbutton = null;

                private int _ct;
                private int _cb;

                private double _scale;
                private double _margin_size;  //used by maximum/minimum price in dataseries and also by auto scroll feature.
                private double _un_margin;
            
                private int _center_margin_ticks = 2;    // margin in ticks within which center AutoScroll type will keep current close.
                private double _center_margin;

                private double _pr_min;
                private double _pr_max;
                private double _old_prmin;
                private double _old_prmax;

                private double _space_max;
                private double _space_min;

                private bool _init_scale;

                private bool _auto_scroll = true;
                private bool _vertical_drag = true;

                private double _as_margin = 0;   // margin to cover in autoscroll/verticaldrag mode
                private AutoScrollButtonPosition _asbp = AutoScrollButtonPosition.TOP_RIGHT;
                private AutoScrollType _ast = AutoScrollType.Extreem;

                private int _old_mouse_y;
                private int _speed = 2;
                private int _extra_margin = 2;

                private bool _show_as = true;
                private bool _show_vs = false;
                private bool _show_vd = false;
            #endregion

            #region Right Panel
            private bool _buttonsloaded = false;
            private ToolStrip _strip = null;
            private Control[] _controls = null;
            private ToolStripSeparator _sepL = null;
            private ToolStripSeparator _sepR = null;

            
            private Font _boldFont = new Font("Arial", 8, FontStyle.Bold);
            private ToolStripButton _btn_rpanel = null;

            private bool _fright_panel = false;
            private Panel _right_panel = null;
            private Label _label_panel = null;
            private TabControl _main_tabcontrol = null;
            private TabPage _tabES = null;

            public GTxtParser pdfES;
            private String pathINI;
            private Label textPathINI;
            private Button butESLoad;
            private Button butESLoadLevels;
            #endregion

            #region BarTimer
                private string _errorDisabled = "Bar timer disabled since either you are disconnected or current time is outside session time or chart end date";
                private string _errorIntraday = "Bar timer only works on intraday TIME based intervals";
                private string _timeLeft = String.Empty;

                private StringFormat _stringFormat = new StringFormat();
                private Font _textFont = new Font("Verdana", 15.75F);
                private Font _textFont2 = new Font("Bodoni MT Condensed", 9F);
                private int _textWidth = 0;
                private int _textHeight = 0;
                private float _noTickTextWidth = 0;
                private float _noTickTextHeight = 0;
                private float _noConTextWidth = 0;
                private float _noConTextHeight = 0;
                
                private Color _textColor = Color.Green;
                private SolidBrush _textBrush = new SolidBrush(Color.Transparent);

                private Timer _updateTimer = new Timer();
                private DateTime _lastTimePlot = Cbi.Globals.MinDate;

                private bool _updateT = true;
                
                private ObjectPosition _thisPosition = ObjectPosition.TopLeft;
                private DisplayType _displayType = DisplayType.All;

                private String _separator = ":";

                private int _var1 = 0;
		        private int _var2 = 0;
            #endregion

            #region PriceLine
                private Color _lineColorPL = Color.Black;
                private int _lineWidthPL = 1;
                private DashStyle _lineStylePL = DashStyle.Dot;
                private bool _useLinePL = false;
                private int _rayLengthPL = 1;
            #endregion

            #region BigPrint
                private int _bigPrintVolume = 100;
                private String _bigPrintSound = @"alert1.wav";
            #endregion

            #region Levels Call & Put           
            private List<GTxtParser.LEVELS_CALL_PUT> _lc;
            private List<GTxtParser.LEVELS_CALL_PUT> _lp;
            private List<GTxtParser.LEVELS_INDEX> _li;
            private string _sessionDate;
            private DateTime _startSession;
            private DateTime _stopSession;

            private double _clow, _cclow;
            private double _phigh, _pphigh;
            private bool _cf;
            private bool _pf;
            #endregion
        #endregion

        private void initControls()
        {
            #region Create ES-Options
            _right_panel = new Panel();
            _label_panel = new Label();
            _main_tabcontrol = new TabControl();
            _tabES = new TabPage();

            pathINI = @"c:\nt\es.ini";

            pdfES = new GTxtParser(pathINI);
            textPathINI = new Label();
            butESLoad = new Button();
            butESLoadLevels = new Button();
            #endregion
        }

        private void butESLoad_Click(object sender, EventArgs e)
        {
            butESLoad.Hide();
            pdfES.Parser();
            butESLoadLevels.Show();           
        }

        private void butESLoadLevels_Click(object sender, EventArgs e)
        {
            //cnt; month; open interest st; open interest end; oi change; open_range; delta st; delta end
            /*pdfES.BuildCallLevels(10, "2017-02-01", (DateTime.Now.Day <= 15) ? DateTime.Now.Month : DateTime.Now.Month, 100, 40000, 5, 3, 0.1, 0.600);
            pdfES.BuildCallLevels(10, "2017-02-02", (DateTime.Now.Day <= 15) ? DateTime.Now.Month : DateTime.Now.Month, 100, 40000, 5, 3, 0.1, 0.600);
            pdfES.BuildCallLevels(10, "2017-02-03", (DateTime.Now.Day <= 15) ? DateTime.Now.Month : DateTime.Now.Month, 100, 40000, 5, 3, 0.1, 0.600);
            pdfES.BuildCallLevels(10, "2017-02-06", (DateTime.Now.Day <= 15) ? DateTime.Now.Month : DateTime.Now.Month, 100, 40000, 5, 3, 0.1, 0.600);
            pdfES.BuildCallLevels(10, "2017-02-07", (DateTime.Now.Day <= 15) ? DateTime.Now.Month : DateTime.Now.Month, 100, 40000, 5, 3, 0.1, 0.600);
            pdfES.BuildCallLevels(10, "2017-02-08", (DateTime.Now.Day <= 15) ? DateTime.Now.Month : DateTime.Now.Month, 100, 40000, 5, 3, 0.1, 0.600);
            pdfES.BuildCallLevels(10, "2017-02-09", (DateTime.Now.Day <= 15) ? DateTime.Now.Month : DateTime.Now.Month, 100, 40000, 5, 0.1, 0.01, 0.600);
            pdfES.BuildCallLevels(10, "2017-02-10", (DateTime.Now.Day <= 15) ? DateTime.Now.Month : DateTime.Now.Month, 100, 40000, 50, 0.1, 0.01, 0.600);
            pdfES.BuildCallLevels(10, "2017-02-13", (DateTime.Now.Day <= 15) ? DateTime.Now.Month : DateTime.Now.Month, 100, 40000, 50, 0.1, 0.01, 0.600);*/
           
            
            pdfES.BuildCallLevels(10, "2017-09-04", DateTime.Now.Month, 300, 50, 0.2);
            pdfES.BuildCallLevels(10, "2017-09-06", DateTime.Now.Month, 300, 50, 0.2);
            pdfES.BuildCallLevels(10, "2017-09-08", DateTime.Now.Month, 300, 50, 0.2);
            pdfES.BuildCallLevels(10, "2017-09-14", DateTime.Now.Month, 300, 50, 0.2);
            pdfES.BuildCallLevels(10, "2017-09-18", DateTime.Now.Month, 300, 50, 0.2);
            pdfES.BuildCallLevels(10, "2017-09-19", DateTime.Now.Month, 300, 50, 0.2);
            pdfES.BuildCallLevels(10, "2017-09-20", DateTime.Now.Month, 300, 50, 0.2);
            pdfES.BuildCallLevels(10, "2017-09-21", DateTime.Now.Month, 300, 50, 0.2);
            pdfES.BuildCallLevels(10, "2017-09-22", DateTime.Now.Month, 300, 50, 0.2);
            /*pdfES.BuildPutLevels(10, "2017-02-01", (DateTime.Now.Day <= 15) ? DateTime.Now.Month : DateTime.Now.Month, 100, 25000, 5, 2, 0.1, 0.500);
            pdfES.BuildPutLevels(10, "2017-02-02", (DateTime.Now.Day <= 15) ? DateTime.Now.Month : DateTime.Now.Month, 100, 25000, 5, 2, 0.1, 0.500);
            pdfES.BuildPutLevels(10, "2017-02-03", (DateTime.Now.Day <= 15) ? DateTime.Now.Month : DateTime.Now.Month, 100, 25000, 5, 2, 0.1, 0.500);
            pdfES.BuildPutLevels(10, "2017-02-06", (DateTime.Now.Day <= 15) ? DateTime.Now.Month : DateTime.Now.Month, 100, 25000, 5, 2, 0.1, 0.500);
            pdfES.BuildPutLevels(10, "2017-02-07", (DateTime.Now.Day <= 15) ? DateTime.Now.Month : DateTime.Now.Month, 100, 25000, 5, 2, 0.1, 0.500);
            pdfES.BuildPutLevels(10, "2017-02-08", (DateTime.Now.Day <= 15) ? DateTime.Now.Month : DateTime.Now.Month, 100, 25000, 5, 2, 0.1, 0.500);
            pdfES.BuildPutLevels(10, "2017-02-09", (DateTime.Now.Day <= 15) ? DateTime.Now.Month : DateTime.Now.Month, 100, 25000, 5, 2, 0.1, 0.500);
            pdfES.BuildPutLevels(10, "2017-02-10", (DateTime.Now.Day <= 15) ? DateTime.Now.Month : DateTime.Now.Month, 100, 25000, 5, 2, 0.1, 0.500);
            pdfES.BuildPutLevels(10, "2017-02-13", (DateTime.Now.Day <= 15) ? DateTime.Now.Month : DateTime.Now.Month, 100, 25000, 5, 2, 0.1, 0.500);*/


            pdfES.BuildPutLevels(10, "2017-09-04", DateTime.Now.Month, 500, 300, 0.1);
            pdfES.BuildPutLevels(10, "2017-09-06", DateTime.Now.Month, 500, 300, 0.1);
            pdfES.BuildPutLevels(10, "2017-09-08", DateTime.Now.Month, 500, 300, 0.1);
            pdfES.BuildPutLevels(10, "2017-09-14", DateTime.Now.Month, 500, 300, 0.1);
            pdfES.BuildPutLevels(10, "2017-09-18", DateTime.Now.Month, 500, 300, 0.1);
            pdfES.BuildPutLevels(10, "2017-09-19", DateTime.Now.Month, 500, 300, 0.1);
            pdfES.BuildPutLevels(10, "2017-09-20", DateTime.Now.Month, 500, 300, 0.1);
            pdfES.BuildPutLevels(10, "2017-09-21", DateTime.Now.Month, 500, 300, 0.1);
            pdfES.BuildPutLevels(10, "2017-09-22", DateTime.Now.Month, 500, 300, 0.1);
            pdfES.GetCallLevels();
            pdfES.GetPutLevels();

            /*gridES.Redim(0, 0);
            gridES.Redim(pdfES.Levels_Call.Count + pdfES.Levels_Put.Count + 1, 5);

            gridES[0, 0] = new SourceGrid.Cells.ColumnHeader("Strike");
            gridES[0, 1] = new SourceGrid.Cells.ColumnHeader("Level");
            gridES[0, 2] = new SourceGrid.Cells.ColumnHeader("LevelHigh");
            gridES[0, 3] = new SourceGrid.Cells.ColumnHeader("LevelLow");
            gridES[0, 4] = new SourceGrid.Cells.ColumnHeader("Option");

            for (int i = gridES.FixedRows + 1; i < pdfES.Levels_Call.Count + 1; i++)
            {
                gridES[i, 0] = new SourceGrid.Cells.Cell(pdfES.Levels_Call[i - 1].strike);
                gridES[i, 1] = new SourceGrid.Cells.Cell(pdfES.Levels_Call[i - 1].level);
                gridES[i, 2] = new SourceGrid.Cells.Cell(pdfES.Levels_Call[i - 1].level_high);
                gridES[i, 3] = new SourceGrid.Cells.Cell(pdfES.Levels_Call[i - 1].level_low);
                gridES[i, 4] = new SourceGrid.Cells.Cell("Call");
            }

            int j = 0;
            for (int i = pdfES.Levels_Call.Count + 1; i < pdfES.Levels_Call.Count + pdfES.Levels_Put.Count + 1; i++)
            {
                gridES[i, 0] = new SourceGrid.Cells.Cell(pdfES.Levels_Put[j].strike);
                gridES[i, 1] = new SourceGrid.Cells.Cell(pdfES.Levels_Put[j].level);
                gridES[i, 2] = new SourceGrid.Cells.Cell(pdfES.Levels_Put[j].level_high);
                gridES[i, 3] = new SourceGrid.Cells.Cell(pdfES.Levels_Put[j].level_low);
                gridES[i, 4] = new SourceGrid.Cells.Cell("Put");
                j++;
            }

            gridES.AutoSizeCells();
            gridES.Selection.Focus(new SourceGrid.Position(0, 0), true);*/
        }

        protected override void GInitialize()
        {
            Overlay = true;
            AutoScale = true;
            DisplayInDataBox = false;
            PaintPriceMarkers = false;
            _init_scale = false;
            _fright_panel = false;

            initControls();
        }

        protected override void GOnStartUp()
        {
            //base.OnStartUp();

            #region AutoScroll
                this.ChartControl.YAxisRangeTypeRight = YAxisRangeType.Fixed;

                // Vertical Scroll Bar
                _vsbar = new VScrollBar();
                _vsbar.Dock = DockStyle.Right;
                _vsbar.Width = 25;
                _vsbar.Minimum = 0;
                _vsbar.Maximum = 100;
                _vsbar.SmallChange = _speed;
                _vsbar.Value = 0;
                _vsbar.Name = "vsBar";

                if (_show_vs)
                {
                    _vsbar.Show();
                }
                else _vsbar.Hide();

                this.ChartControl.Controls.Add(_vsbar);

                _vsbar.Scroll += new ScrollEventHandler(_vsbar_Scroll);
                this.ChartControl.ChartPanel.MouseUp += new MouseEventHandler(_MouseDrag_OnScale);
                this.ChartControl.ChartPanel.MouseDown += new MouseEventHandler(_MouseDown_OnChart);
                this.ChartControl.ChartPanel.MouseMove += new MouseEventHandler(_MouseDrag_OnChart);

                //Auto Scroll Button
                _asbutton = new Button();

                switch(_asbp)
                {
                    case AutoScrollButtonPosition.TOP_LEFT:
                        {
                            _asbutton.Location = new Point(this.ChartControl.CanvasLeft + 8, this.ChartControl.CanvasTop + 15);
                            break;
                        }

                    case AutoScrollButtonPosition.TOP_RIGHT:
                        {
                            _asbutton.Location = new Point(this.ChartControl.CanvasRight - 75, this.ChartControl.CanvasTop + 3);
                            break;
                        }
                }

                _asbutton.Size = new Size(27, 27);
                _asbutton.BackColor = Color.GreenYellow;
                _asbutton.Name = "asButton";
                _asbutton.Text = "AS";

                if(_show_as)
                {
                    _asbutton.Show();
                }
                else _asbutton.Hide();

                this.ChartControl.ChartPanel.Controls.Add(_asbutton);
                _asbutton.Click += _ToggleAutoScroll;

                //Vertical Drag Button
                _vdbutton = new Button();

                switch(_asbp)
                {
                    case AutoScrollButtonPosition.TOP_LEFT:
                        {
                            _vdbutton.Location = new Point(this.ChartControl.CanvasLeft + 38, this.ChartControl.CanvasTop + 15);
                            break;
                        }

                    case AutoScrollButtonPosition.TOP_RIGHT:
                        {
                            _vdbutton.Location = new Point(this.ChartControl.CanvasRight - 105, this.ChartControl.CanvasTop + 3);
                            break;
                        }
                }

                _vdbutton.Size = new Size(27, 27);
                _vdbutton.BackColor = Color.GreenYellow;
                _vdbutton.Name = "vdButton";
                _vdbutton.Text = "VD";

                if(_show_vd)
                {
                    _vdbutton.Show();
                }
                else _vdbutton.Hide();

                this.ChartControl.ChartPanel.Controls.Add(_vdbutton);
                _vdbutton.Click += _ToggleVerticalDrag;


                _old_prmin = double.MaxValue;
                _old_prmax = double.MinValue;
                _margin_size = (double)(_extra_margin * TickSize);
                _center_margin = (double)(_center_margin_ticks * TickSize) / 2;
                UpdateScale();
            #endregion Scroll

            #region Right Panel
            Control[] controls = ChartControl.Controls.Find("tsrTool", false);

            if (controls.Length > 0)
            {
                _strip = (ToolStrip)controls[0];

                _sepL = new ToolStripSeparator();
                _sepL.Alignment = ToolStripItemAlignment.Left;
                _strip.Items.Add(_sepL);

                _btn_rpanel = new ToolStripButton("btn_rpanel");
                _btn_rpanel.Font = _boldFont;
                _btn_rpanel.ForeColor = Color.Green;
                _btn_rpanel.BackColor = Color.Gainsboro;
                _btn_rpanel.Alignment = ToolStripItemAlignment.Right;
                _btn_rpanel.Text = "RP";
                _btn_rpanel.ToolTipText = "Правая панель";
                _btn_rpanel.Click += _btn_rpanel_Click;
                _strip.Items.Add(_btn_rpanel);

                _sepR = new ToolStripSeparator();
                _sepR.Alignment = ToolStripItemAlignment.Right;
                _strip.Items.Add(_sepR);

                _buttonsloaded = true;
            }

            _tabES.Location = new Point(4, 25);
            _tabES.Name = "tabES";
            _tabES.Padding = new Padding(3);
            _tabES.TabIndex = 1;
            _tabES.Text = "ES Levels";
            _tabES.UseVisualStyleBackColor = true;

            #region ES-Options
            textPathINI.ForeColor = Color.Red;
            textPathINI.AutoSize = true;
            textPathINI.Location = new Point(6, 6);
            textPathINI.Name = "pathINI";
            textPathINI.Size = new Size(33, 13);
            textPathINI.TabIndex = 0;
            textPathINI.Text = "IniFile: " + pathINI;       

            /*gridES.Name = "ES-Options";
            gridES.TabIndex = 1;
            gridES.Location = new Point(6, 30);
            gridES.Size = new Size(200, 256);
            gridES.BorderStyle = BorderStyle.FixedSingle;
            */
            // Button ES loading
            butESLoad.BackColor = SystemColors.ButtonShadow;
            butESLoad.ForeColor = Color.Green;
            butESLoad.Location = new Point(6, 35);
            butESLoad.Name = "ESLoad";
            butESLoad.Size = new Size(100, 25);
            butESLoad.TabIndex = 2;
            butESLoad.Text = "ES loading...";
            butESLoad.UseVisualStyleBackColor = true;
            butESLoad.Click += new EventHandler(butESLoad_Click);

            // Button ES loading levels
            butESLoadLevels.BackColor = SystemColors.ButtonShadow;
            butESLoadLevels.ForeColor = Color.Green;
            butESLoadLevels.Location = new Point(6, 35);
            butESLoadLevels.Name = "ESLoad";
            butESLoadLevels.Size = new Size(150, 25);
            butESLoadLevels.TabIndex = 3;
            butESLoadLevels.Text = "ES levels loading...";
            butESLoadLevels.UseVisualStyleBackColor = true;
            butESLoadLevels.Hide();
            butESLoadLevels.Click += new EventHandler(butESLoadLevels_Click);

            #region tabESLoad
            _tabES.Controls.Add(textPathINI);
            //_tabES.Controls.Add(gridES);
            _tabES.Controls.Add(butESLoad);
            _tabES.Controls.Add(butESLoadLevels);
            #endregion
            #endregion

            //main tabcontrol
            //main_tabcontrol.Controls.Add(tabSwingLength);
            //main_tabcontrol.Controls.Add(tabSwingRelation);
            _main_tabcontrol.Name = "main_tabcontrol";
            _main_tabcontrol.Dock = DockStyle.Fill;
            _main_tabcontrol.Location = new Point(0, 0);
            _main_tabcontrol.Multiline = true;
            _main_tabcontrol.Padding = new Point(3, 3);
            _main_tabcontrol.SelectedIndex = 3;
            _main_tabcontrol.TabIndex = 0;
            _main_tabcontrol.Controls.Add(_tabES);

            _right_panel.Controls.Add(this._main_tabcontrol);

            //right panel
            _right_panel.BackColor = Color.White;
            _right_panel.BorderStyle = BorderStyle.Fixed3D;
            _right_panel.Dock = DockStyle.Right;
            _right_panel.Location = new Point(0, 0);
            _right_panel.MinimumSize = new Size(250, 0);
            _right_panel.Name = "right_panel";
            _right_panel.TabIndex = 0;
            _right_panel.Size = new System.Drawing.Size(250, ChartControl.Height);   
            _right_panel.Hide();
            _fright_panel = false;

            ChartControl.Controls.Add(_right_panel);
            #endregion

            #region BarTimer
            _textFont2 = new Font(_textFont, FontStyle.Bold);
                _textBrush = new SolidBrush(_textColor);    
                _updateTimer.Tick += new EventHandler(_OnUpdateTimerTick);
                _updateTimer.Interval = 100;
                _updateTimer.Start();
                this.ZOrder = 1;
            #endregion

            _clow = _cclow = 0.0;
            _phigh = _pphigh = 0.0;
            _cf = false;
            _pf = false;
        
            DrawLevels();
        }

        protected override void GOnBarUpdate()
        {
            #region Scroll
            if (CurrentBar < 0) return;

            _pr_min = MIN(Low, Count)[0] - _margin_size - _un_margin;
            _pr_max = MAX(High, Count)[0] + _margin_size;

            if (_pr_min < _old_prmin || _pr_max > _old_prmax)
            {
                SetScrollLimits();

                _old_prmin = _pr_min;
                _old_prmax = _pr_max;
            }

            if (!_init_scale && CurrentBar == Count - 1)
            {
                InitScale();
                UpdateScale();

                _init_scale = true;
            }

            AutoScroll();
            #endregion

            #region PriceLine
            if (_rayLengthPL > 20) _rayLengthPL = 20;

                if (_useLinePL)
                    DrawHorizontalLine("Currprice", false, Close[0], _lineColorPL, _lineStylePL, _lineWidthPL);
                else
                    if (CurrentBar > 50)
                        DrawRay("CurrRay", false, _rayLengthPL, Close[0], 0, Close[0], _lineColorPL, _lineStylePL, _lineWidthPL);
            #endregion

            if (CurrentBar > 10)
            {
                DrawEntranceExit();
            }
        }

        #region Draw Call & Put
        public void DrawLevels()
        {
            foreach (KeyValuePair<int, DateTime> d in pdfES.Dates)
            {
                _sessionDate = d.Value.ToString("yyyy-MM-dd");            
                _lc = pdfES.GetCallLevelsDate(_sessionDate);
                _lp = pdfES.GetPutLevelsDate(_sessionDate);
                _li = pdfES.GetIndexLevelsDate(_sessionDate);
                Bars.Session.GetNextBeginEnd(d.Value.AddDays(1), out _startSession, out _stopSession);

                double div = 0.0;

                #region CALL
                for (int i = 0; i < _lc.Count; i++)
                {
                    if (_lc[i].tradedate.ToString("yyyy-MM-dd") == _sessionDate)
                    {
                        if (_lc[i].volume == 0) continue;

                        div = _lc[i].oi / _lc[i].volume;
                        DrawRectangle("CallR " + _lc[i].strike + _startSession.ToString("yyyy-MM-dd"), true, _startSession, _lc[i].level_low, _stopSession, _lc[i].level_high, Color.Red, Color.Pink, 2);
                        DrawLine("CallL " + _lc[i].strike + _startSession.ToString("yyyy-MM-dd"), true, _startSession, _lc[i].level, _stopSession, _lc[i].level, Color.Red, DashStyle.Solid, 2);                       
                        DrawText("CallOI" + _lc[i].strike + _startSession.ToString("yyyy-MM-dd"), true, "#Открытый интерес: " + _lc[i].oi, _startSession, _lc[i].level_low, -10, Color.DarkRed, new Font("Tahoma", 6F, FontStyle.Regular), StringAlignment.Near, Color.Transparent, Color.Transparent, 0);
                        DrawText("CallVOL" + _lc[i].strike + _startSession.ToString("yyyy-MM-dd"), true, "#Объем: " + _lc[i].volume, _startSession, _lc[i].level_low, -25, Color.DarkRed, new Font("Tahoma", 6F, FontStyle.Regular), StringAlignment.Near, Color.Transparent, Color.Transparent, 0);
                        DrawText("CallDIV" + _lc[i].strike + _startSession.ToString("yyyy-MM-dd"), true, "#K: " + Math.Round(div, 3), _startSession, _lc[i].level_low, -40, Color.DarkRed, new Font("Tahoma", 6F, FontStyle.Regular), StringAlignment.Near, Color.Transparent, Color.Transparent, 0);
                        
                        //DrawText("CallSETT" + _lc[i].strike + _startSession.ToString("yyyy-MM-dd"), true, "#Премия: " + Math.Round(_lc[i].sett_price, 2), _startSession, _lc[i].level_low, -25, Color.DarkGreen, new Font("Tahoma", 6F, FontStyle.Regular), StringAlignment.Near, Color.Transparent, Color.Transparent, 0);
                        //DrawText("CallOI_SP" + _lc[i].strike + _startSession.ToString("yyyy-MM-dd"), true, "#ОИ*Премия: " + Math.Round(_lc[i].oi * _lc[i].sett_price, 2), _startSession, _lc[i].level_low, -55, Color.DarkGreen, new Font("Tahoma", 6F, FontStyle.Regular), StringAlignment.Near, Color.Transparent, Color.Transparent, 0);
                        //DrawText("CallST" + _lc[i].strike + _startSession.ToString("yyyy-MM-dd"), true, "#Strike: " + _lc[i].strike, _startSession, _lc[i].level_low, -70, Color.DarkGreen, new Font("Tahoma", 6F, FontStyle.Regular), StringAlignment.Near, Color.Transparent, Color.Transparent, 0);
                        DrawLine("CallS " + _lc[i].strike + _startSession.ToString("yyyy-MM-dd"), true, _startSession, _lc[i].strike, _stopSession, _lc[i].strike, Color.Red, DashStyle.Dot, 2);                       
                    }
                }
                #endregion
                
                #region PUT
                for (int i = 0; i < _lp.Count; i++)
                {
                    if (_lp[i].tradedate.ToString("yyyy-MM-dd") == _sessionDate)
                    {
                        if (_lp[i].volume == 0) continue;

                        div = _lp[i].oi / _lp[i].volume;
                        DrawRectangle("PutR " + _lp[i].strike + _startSession.ToString("yyyy-MM-dd"), true, _startSession, _lp[i].level_low, _stopSession, _lp[i].level_high, Color.Green, Color.PaleGreen, 2);
                        DrawLine("PutL " + _lp[i].strike + _startSession.ToString("yyyy-MM-dd"), true, _startSession, _lp[i].level, _stopSession, _lp[i].level, Color.Green, DashStyle.Solid, 2);
                        DrawText("PutOI" + _lp[i].strike + _startSession.ToString("yyyy-MM-dd"), true, "#Открытый интерес: " + _lp[i].oi, _startSession, _lp[i].level_high, -10, Color.DarkGreen, new Font("Tahoma", 6F, FontStyle.Regular), StringAlignment.Near, Color.Transparent, Color.Transparent, 0);
                        DrawText("PutVOL" + _lp[i].strike + _startSession.ToString("yyyy-MM-dd"), true, "#Объем: " + _lp[i].volume, _startSession, _lp[i].level_high, -25, Color.DarkGreen, new Font("Tahoma", 6F, FontStyle.Regular), StringAlignment.Near, Color.Transparent, Color.Transparent, 0);
                        DrawText("PutDIV" + _lp[i].strike + _startSession.ToString("yyyy-MM-dd"), true, "#K: " + Math.Round(div, 3), _startSession, _lp[i].level_low, -40, Color.DarkGreen, new Font("Tahoma", 6F, FontStyle.Regular), StringAlignment.Near, Color.Transparent, Color.Transparent, 0);
                        
                        //DrawText("PutSETT" + _lp[i].strike + _startSession.ToString("yyyy-MM-dd"), true, "#Премия: " + Math.Round(_lp[i].sett_price, 2), _startSession, _lp[i].level_high, -25, Color.DarkRed, new Font("Tahoma", 6F, FontStyle.Regular), StringAlignment.Near, Color.Transparent, Color.Transparent, 0);
                        //DrawText("PutOI_SP" + _lp[i].strike + _startSession.ToString("yyyy-MM-dd"), true, "#ОИ*Премия: " + Math.Round(_lp[i].oi * _lp[i].sett_price, 2), _startSession, _lp[i].level_high, -55, Color.DarkRed, new Font("Tahoma", 6F, FontStyle.Regular), StringAlignment.Near, Color.Transparent, Color.Transparent, 0);
                        //DrawText("PutST" + _lp[i].strike + _startSession.ToString("yyyy-MM-dd"), true, "#Strike: " + _lp[i].strike, _startSession, _lp[i].level_high, -70, Color.DarkRed, new Font("Tahoma", 6F, FontStyle.Regular), StringAlignment.Near, Color.Transparent, Color.Transparent, 0);
                        DrawLine("PutS " + _lp[i].strike + _startSession.ToString("yyyy-MM-dd"), true, _startSession, _lp[i].strike, _stopSession, _lp[i].strike, Color.Green, DashStyle.Dot, 2);
                    }
                }
                #endregion

                #region INDEX
                if (_li.Count > 0)
                    if (_li[0].tradedate.ToString("yyyy-MM-dd") == _sessionDate)
                    {
                        DrawLine("IndexPitHigh" + _li[0].tradedate.ToString("yyyy-MM-dd"), true, _startSession, _li[0].pit_high, _stopSession, _li[0].pit_high, Color.PaleVioletRed, DashStyle.Solid, 3);
                        DrawLine("IndexPitLow" + _li[0].tradedate.ToString("yyyy-MM-dd"), true, _startSession, _li[0].pit_low, _stopSession, _li[0].pit_low, Color.CadetBlue, DashStyle.Solid, 3);
                    }
                #endregion
            }
        }

        public void DrawEntranceExit()
        {
            foreach (KeyValuePair<int, DateTime> d in pdfES.Dates)
            {
                _sessionDate = d.Value.ToString("yyyy-MM-dd");
                
                if (_sessionDate == Bars.GetTradingDayFromLocal(Time[0]).ToString("yyyy-MM-dd"))
                {
                    List<GTxtParser.LEVELS_CALL_PUT> _tlc = pdfES.GetCallLevelsDate(_sessionDate);
                    List<GTxtParser.LEVELS_CALL_PUT> _tlp = pdfES.GetPutLevelsDate(_sessionDate);

                    _tlc.Sort(delegate(GTxtParser.LEVELS_CALL_PUT cp1, GTxtParser.LEVELS_CALL_PUT cp2) { return cp1.strike.CompareTo(cp2.strike); });
                    _tlp.Sort(delegate(GTxtParser.LEVELS_CALL_PUT cp1, GTxtParser.LEVELS_CALL_PUT cp2) { return cp2.strike.CompareTo(cp1.strike); });
                    
                    for (int i = 0; i < _tlc.Count; i++)
                    {
                        if (_tlc[i].strike >= Open[0])
                        {
                            _clow = _tlc[i].strike;                          
                            _cf = true;
                            break;
                        }
                    }
                    
                    for (int i = 0; i < _tlp.Count; i++)
                    {
                        if (_tlp[i].strike <= Open[0])
                        {
                            _phigh = _tlp[i].strike;
                            _pf = true;
                            break;
                        }
                    }
         
                    if (Open[0] <= _clow && Open[1] >= Close[1] && Open[0] > Close[0] && _cf)
                    {
                        if (Close[2] >= _clow || Low[2] >= _clow)
                        {
                            DrawTriangleDown("TriangleDownR" + CurrentBar, true, 2, High[2] + TickSize * 4, Color.Red);
                            DrawTriangleDown("TriangleDownRR" + CurrentBar, true, 0, High[0] + TickSize * 2, Color.Magenta);
                        }

                        _cf = false;
                     }

                    if (Open[0] >= _phigh && Open[1] <= Close[1] && Close[0] > Open[0] && _pf)
                    {
                        if (Close[2] <= _phigh || High[2] <= _phigh)
                        {
                            DrawTriangleUp("TriangleUpG" + CurrentBar, true, 2, Low[2] - TickSize * 4, Color.Green);
                            DrawTriangleUp("TriangleUpGG" + CurrentBar, true, 0, Low[0] - TickSize * 2, Color.Lime);
                        }

                        _pf = false;
                    }

                    List<GTxtParser.LEVELS_CALL_PUT> _tlpp = pdfES.GetCallLevelsDate(_sessionDate);
                    List<GTxtParser.LEVELS_CALL_PUT> _tlcc = pdfES.GetPutLevelsDate(_sessionDate);

                    _tlpp.Sort(delegate(GTxtParser.LEVELS_CALL_PUT cp1, GTxtParser.LEVELS_CALL_PUT cp2) { return cp2.strike.CompareTo(cp1.strike); });
                    _tlcc.Sort(delegate(GTxtParser.LEVELS_CALL_PUT cp1, GTxtParser.LEVELS_CALL_PUT cp2) { return cp1.strike.CompareTo(cp2.strike); });

                    for (int i = 0; i < _tlpp.Count; i++)
                    {
                        if (_tlpp[i].strike <= Open[0])
                        {
                            _pphigh = _tlpp[i].strike;
                            _pf = true;
                            break;
                        }
                    }

                    if (Open[0] >= _pphigh && Open[1] <= Close[1] && Close[0] > Open[0] && _pf)
                    {
                        if (Close[2] <= _pphigh || High[2] <= _pphigh)
                        {
                            DrawTriangleUp("TriangleUpG" + CurrentBar, true, 2, Low[2] - TickSize * 4, Color.Green);
                            DrawTriangleUp("TriangleUpGG" + CurrentBar, true, 0, Low[0] - TickSize * 2, Color.Lime);
                        }

                        _pf = false;
                    }

                    for (int i = 0; i < _tlcc.Count; i++)
                    {
                        if (_tlcc[i].strike >= Open[0])
                        {
                            _cclow = _tlcc[i].strike;
                            _cf = true;
                            break;
                        }
                    }

                    if (Open[0] <= _cclow && Open[1] >= Close[1] && Open[0] > Close[0] && _cf)
                    {
                        if (Close[2] >= _cclow || Low[2] >= _cclow)
                        {
                            DrawTriangleDown("TriangleDownR" + CurrentBar, true, 2, High[2] + TickSize * 4, Color.Red);
                            DrawTriangleDown("TriangleDownRR" + CurrentBar, true, 0, High[0] + TickSize * 2, Color.Magenta);
                        }

                        _cf = false;
                    }
                }
            }
        }
        #endregion

        #region Events
        private void _MouseDrag_OnScale(object sender, MouseEventArgs e)
        {
            if(this.ChartControl.CanvasRight < e.X && e.X < this.ChartControl.Size.Width)
            {
                if(e.Button == MouseButtons.Left)
                {
                    UpdateScale();
                    SetScrollLimits();
                    this.ChartControl.ChartPanel.Invalidate();
                }
            }
        }

        private void _MouseDown_OnChart(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                if(!MouseOnChart(e.X, e.Y)) return;
                else _old_mouse_y = e.Y;
            }
        }

        private void _MouseDrag_OnChart(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _vertical_drag)
            {
                if(!MouseOnChart(e.X, e.Y)) return;
                else
                {
                    if(_auto_scroll && _vertical_drag)
                    {
                        _auto_scroll = false;
                        _asbutton.BackColor = Color.LightSalmon;
                    }

                    _as_margin = (double)(e.Y - _old_mouse_y) * _scale;
                    this.ChartControl.FixedPanelMinRight = this.ChartControl.FixedPanelMinRight + _as_margin;
                    this.ChartControl.FixedPanelMaxRight = this.ChartControl.FixedPanelMaxRight + _as_margin;
                    _as_margin = 0;
                    SetScrollLimits();
                    _old_mouse_y = e.Y;
                }
            }

            this.ChartControl.ChartPanel.Invalidate();
        }

        private void _vsbar_Scroll(object sender, ScrollEventArgs se)
        {
            if(_auto_scroll)
            {
                _auto_scroll = false;
                _asbutton.BackColor = Color.LightSalmon;
            }

            DoScroll(_vsbar.Value);
        }

        private void _ToggleAutoScroll(object sender, EventArgs e)
        {
            if (!_auto_scroll)
            {
                _auto_scroll = true;
                _asbutton.BackColor = Color.GreenYellow;
                AutoScroll();          // added to make shift of chart when clicks.
            }

            else if(_auto_scroll)
            {
                _auto_scroll = false;
                _asbutton.BackColor = Color.LightSalmon;
            }

            this.ChartControl.ChartPanel.Invalidate();
        }

        private void _ToggleVerticalDrag(object sender, EventArgs e)
        {
            if(!_vertical_drag)
            {
                _vertical_drag = true;
                _vdbutton.BackColor = Color.GreenYellow;
            }

            else if(_vertical_drag)
            {
                _vertical_drag = false;
                _vdbutton.BackColor = Color.LightSalmon;
            }

            this.ChartControl.ChartPanel.Invalidate();
        }

        protected bool MouseOnChart(int x, int y)
        {
            int temp_y;
            temp_y = this.ChartControl.GetYByValue(this, this.ChartControl.FixedPanelMinRight);   // restricted only in price panel
            
            if (x < this.ChartControl.CanvasLeft || x >= this.ChartControl.CanvasRight || y < this.ChartControl.CanvasTop || y > temp_y) return false;
            else return true;
        }
        #endregion

        private void _btn_rpanel_Click(object sender, EventArgs e)
        {
            if (_fright_panel == false)
            {
                _right_panel.Show();
                _fright_panel = true;
            }
            else
            {
                _right_panel.Hide();
                _fright_panel = false;
            }
        }

        #region BarTimer
            private void _OnUpdateTimerTick(object sender, EventArgs e)
            {
                if (DateTime.Now.Subtract(_lastTimePlot).Seconds >= 1 && _DisplayTime())
                {
                    ChartControl.ChartPanel.Invalidate();
                    _lastTimePlot = DateTime.Now;
                }
            }
        #endregion

        #region Tools
        protected void InitScale()
        {
            int lmb_idx;       // left most bar index
            int rmb_idx;       // right most bar index

            lmb_idx = CurrentBar - this.FirstBarIndexPainted;
            rmb_idx = CurrentBar - this.LastBarIndexPainted;

            double screen_high = double.MinValue;
            double screen_low = double.MaxValue;

            for (int i = rmb_idx; i <= lmb_idx; i++)
            {
                if (High[i] > screen_high) screen_high = High[i];
                if (Low[i] < screen_low) screen_low = Low[i];
            }

            /*this.ChartControl.YAxisRangeTypeRight = YAxisRangeType.Automatic;
            this.ChartControl.AutoScaleDateRangeTypeRight = AutoScaleDateRangeType.ScreenDateRange;
            this.ChartControl.AutoScaleMarginTypeRight = AutoScaleMarginType.Percent;
            this.ChartControl.FixedPanelMinRight =  screen_low - this.ChartControl.AutoScaleMarginLowerRight;
            this.ChartControl.FixedPanelMaxRight =  screen_high + this.ChartControl.AutoScaleMarginUpperRight;
            this.ChartControl.YAxisRangeTypeRight = YAxisRangeType.Fixed;*/

            this.ChartControl.FixedPanelMinRight = screen_low - 10 * TickSize;
            this.ChartControl.FixedPanelMaxRight = screen_high + 10 * TickSize;
            this.ChartControl.ChartPanel.Invalidate();
        }

        protected void UpdateScale()
        {
            _ct = this.ChartControl.CanvasTop;
            _cb = this.ChartControl.CanvasBottom;
            _scale = (this.ChartControl.FixedPanelMaxRight - this.ChartControl.FixedPanelMinRight) / (_cb - _ct);
            _un_margin = (double)((_vsbar.LargeChange - 1) * _scale);
        }

        protected void SetScrollLimits()
        {
            _space_max = Math.Max(_pr_max, this.ChartControl.FixedPanelMaxRight);
            _space_min = Math.Min(_pr_min, this.ChartControl.FixedPanelMinRight);

            _vsbar.SuspendLayout();
            _vsbar.Minimum = _ct + (int)((this.ChartControl.FixedPanelMaxRight - _space_max) / _scale);
            _vsbar.Maximum = _ct + (int)((this.ChartControl.FixedPanelMinRight - _space_min) / _scale);
            _vsbar.Value = Math.Max(_vsbar.Minimum, 0);
            _vsbar.ResumeLayout();
        }

        protected void DoScroll(int vsvalue)
        {
            this.ChartControl.FixedPanelMaxRight = _space_max - _scale * (double)(vsvalue - _vsbar.Minimum);
            this.ChartControl.FixedPanelMinRight = _space_min + _scale * (double)(_vsbar.Maximum - vsvalue);
            this.ChartControl.ChartPanel.Invalidate();
        }

        protected void AutoScroll()
        {
            int rmb_index;

            rmb_index = CurrentBar - this.LastBarIndexPainted;

            if(this.ChartControl.FixedPanelMaxRight - this.ChartControl.FixedPanelMinRight < High[rmb_index] - Low[rmb_index] + 2 * _margin_size + _un_margin) return;
            if(_auto_scroll)
            {
                switch (_ast)
                {
                    case AutoScrollType.Extreem:
                        {
                            if(this.ChartControl.FixedPanelMaxRight - High[rmb_index] < _margin_size)
                            {
                                _as_margin = (_margin_size - this.ChartControl.FixedPanelMaxRight + High[rmb_index]);
                                this.ChartControl.FixedPanelMaxRight = this.ChartControl.FixedPanelMaxRight + _as_margin;
                                this.ChartControl.FixedPanelMinRight = this.ChartControl.FixedPanelMinRight + _as_margin;

                            }

                            if(Low[rmb_index] - this.ChartControl.FixedPanelMinRight < _margin_size + _un_margin)
                            {
                                _as_margin = (_margin_size + _un_margin - Low[rmb_index] + this.ChartControl.FixedPanelMinRight);
                                this.ChartControl.FixedPanelMinRight = this.ChartControl.FixedPanelMinRight - _as_margin;
                                this.ChartControl.FixedPanelMaxRight = this.ChartControl.FixedPanelMaxRight - _as_margin;

                            }
                            break;
                        }

                    case AutoScrollType.Center:
                        {
                            double center_price;

                            center_price = (this.ChartControl.FixedPanelMaxRight + this.ChartControl.FixedPanelMinRight) / 2.0;
                            
                            if(Close[rmb_index] > center_price + _center_margin)
                            {
                                _as_margin = Close[rmb_index] - center_price - _center_margin;
                                this.ChartControl.FixedPanelMinRight = this.ChartControl.FixedPanelMinRight + _as_margin;
                                this.ChartControl.FixedPanelMaxRight = this.ChartControl.FixedPanelMaxRight + _as_margin;

                            }

                            if(Close[rmb_index] < center_price - _center_margin)
                            {
                                _as_margin = center_price - _center_margin - Close[rmb_index];
                                this.ChartControl.FixedPanelMinRight = this.ChartControl.FixedPanelMinRight - _as_margin;
                                this.ChartControl.FixedPanelMaxRight = this.ChartControl.FixedPanelMaxRight - _as_margin;

                            }
                            break;
                        }
                }

                _as_margin = 0;
                SetScrollLimits();
            }
        }

        #region BarTimer
            private bool _DisplayTime()
            {
                if (ChartControl != null
                        && Bars != null
                        && Bars.Count > 0
                        && Bars.MarketData != null
                        && Bars.MarketData.Connection.PriceStatus == Cbi.ConnectionStatus.Connected
                        && (Bars.MarketData.Connection.Options.Provider != Cbi.Provider.OpenTick || !(Bars.MarketData.Connection.Options as Cbi.OpenTickOptions).UseDelayedData))
                    return true;

                return false;
            }

            private DateTime _Now
            {
                get
                {
                    DateTime now = (Bars.MarketData.Connection.Options.Provider == Cbi.Provider.Replay ? Bars.MarketData.Connection.Now : DateTime.Now);

                    if (now.Millisecond > 0)
                        now = Cbi.Globals.MinDate.AddSeconds((long)System.Math.Floor(now.Subtract(Cbi.Globals.MinDate).TotalSeconds));

                    return now;
                }
            }
        #endregion BarTimer

        #endregion

        #region BigPrint
        public void OnFill(double firstFill, double lastFill, int volume, DateTime time)
        {

        }
        #endregion

        public override void Plot(Graphics g, Rectangle bounds, double min, double max)
        {
            #region Scroll
            switch (_asbp)
            {
                case AutoScrollButtonPosition.TOP_LEFT:
                    {
                        if(_asbutton != null)
                        {
                            _asbutton.Location = new Point(this.ChartControl.CanvasLeft + 8, this.ChartControl.CanvasTop + 15);
                        }

                        if(_vdbutton != null)
                        {
                            _vdbutton.Location = new Point(this.ChartControl.CanvasLeft + 38, this.ChartControl.CanvasTop + 15);
                        }
                        break;
                    }

                case AutoScrollButtonPosition.TOP_RIGHT:
                    {
                        if (_asbutton != null)
                        {
                            _asbutton.Location = new Point(this.ChartControl.CanvasRight - 75, this.ChartControl.CanvasTop + 3);
                        }

                        if (_vdbutton != null)
                        {
                            _vdbutton.Location = new Point(this.ChartControl.CanvasRight - 105, this.ChartControl.CanvasTop + 3);
                        }
                        break;
                    }
            }
            #endregion

            #region BarTimer
            if (Bars == null) return;

                int ClockPlaceX = 0;
                int ClockPlaceY = 0;

                switch (_thisPosition)
                {

                    case ObjectPosition.BottomRight:
                        ClockPlaceX = bounds.Right - _textWidth;
                        ClockPlaceY = bounds.Bottom - _textHeight;
                        break;

                    case ObjectPosition.BottomLeft:
                        ClockPlaceX = bounds.Left + 5;
                        ClockPlaceY = bounds.Bottom - _textHeight;
                        break;

                    case ObjectPosition.TopRight:
                        ClockPlaceX = bounds.Right - _textWidth;
                        ClockPlaceY = bounds.Y + 5;
                        break;

                    case ObjectPosition.TopLeft:
                        ClockPlaceX = bounds.Left + 5;
                        ClockPlaceY = bounds.Y + 5;
                        break;

                    case ObjectPosition.Centre:
                        ClockPlaceX = bounds.Right - ((bounds.Right - bounds.Left) / 2) - _textWidth / 2;
                        ClockPlaceY = bounds.Bottom - ((bounds.Bottom - bounds.Top) / 2) - _textHeight / 2;
                        break;

                    case ObjectPosition.TopCentre:
                        ClockPlaceY = bounds.Top + 7;
                        ClockPlaceX = bounds.Right - bounds.Width / 2;
                        break;

                    case ObjectPosition.BottomCentre:
                        ClockPlaceY = bounds.Bottom - _textHeight - 1;
                        ClockPlaceX = bounds.Right - bounds.Width / 2;
                        break;
                }

                SizeF noConSize = g.MeasureString(_errorDisabled, _textFont2);
                _noConTextWidth = noConSize.Width + 5;
                _noConTextHeight = noConSize.Height + 5;

                SizeF noTickSize = g.MeasureString(_errorIntraday, _textFont2);
                _noTickTextWidth = noTickSize.Width + 5;
                _noTickTextHeight = noTickSize.Height + 5;

                SizeF size = g.MeasureString(_timeLeft, _textFont2);
                _textWidth = (int)size.Width + 5;
                _textHeight = (int)size.Height;

                if (Bars.Period.Id == PeriodType.Minute || Bars.Period.Id == PeriodType.Second)
                {
                    if(_updateT)
	                {
					    TimeSpan barTimeLeft = Bars.Get(Bars.Count - 1).Time.Subtract(_Now);

				        if(_displayType == DisplayType.LapsedTimeOnly)
						
				        if(Bars.Period.Id == PeriodType.Second)
					        _timeLeft = (barTimeLeft.Ticks < 0 ? "�00" : "" + barTimeLeft.Seconds.ToString("00"));
				        else
					        _timeLeft = (barTimeLeft.Ticks < 0 ? "00�00" : "" + barTimeLeft.Minutes.ToString("00") + _separator + barTimeLeft.Seconds.ToString("00"));
					
				        if(_displayType == DisplayType.All)
						
					        _timeLeft = DateTime.Now.ToString("T")+ "\n[" + (barTimeLeft.Ticks < 0 ? "00"+ _separator + "00]" : "" + barTimeLeft.Minutes.ToString("00") + _separator + barTimeLeft.Seconds.ToString("00")+"]");
				
				        if(_displayType == DisplayType.ClockOnly)
				        _timeLeft = DateTime.Now.ToString("T");
				
				
				        g.DrawString(_timeLeft, _textFont2, _textBrush, ClockPlaceX , ClockPlaceY, _stringFormat);
				
				        if((int)barTimeLeft.TotalSeconds < _var2  && (int)barTimeLeft.TotalSeconds > _var1 )	
			            {
				            if((int)barTimeLeft.TotalSeconds % 2 < 1 )//alternate colors every second
				            {	
				                g.DrawString(_timeLeft, _textFont2, _textBrush, ClockPlaceX , ClockPlaceY, _stringFormat);
				            }				
			            }
	
					    g.DrawString(_timeLeft, _textFont2, _textBrush, ClockPlaceX, ClockPlaceY, _stringFormat);
	                }
			        else
				        g.DrawString(_errorDisabled, _textFont2, _textBrush, bounds.X + bounds.Width - _noConTextWidth, bounds.Y + bounds.Height - _noConTextHeight, _stringFormat);
			    }
			    else//if not seconds or minute charts
			    {
				    g.DrawString(_errorIntraday, _textFont2, _textBrush, bounds.X + bounds.Width - _noTickTextWidth, bounds.Y + bounds.Height - _noTickTextHeight, _stringFormat);
				    
                    if(_updateTimer != null)
					    _updateTimer.Enabled = false;
                }
            #endregion
        }

        protected override void GOnTermination()
        {
            #region Term Scroll
            _vsbar.Scroll -= _vsbar_Scroll;
            this.ChartControl.ChartPanel.MouseUp -= _MouseDrag_OnScale;
            this.ChartControl.ChartPanel.MouseDown -= _MouseDown_OnChart;
            this.ChartControl.ChartPanel.MouseMove -= _MouseDrag_OnChart;

            this.ChartControl.Controls.Remove(_vsbar);
            _vsbar.Dispose();

            if(_asbutton != null)
            {
                _asbutton.Click -= _ToggleAutoScroll;
                this.ChartControl.ChartPanel.Controls.Remove(_asbutton);
                _asbutton.Dispose();
            }

            if(_vdbutton != null)
            {
                _vdbutton.Click -= _ToggleVerticalDrag;
                this.ChartControl.ChartPanel.Controls.Remove(_vdbutton);
                _vdbutton.Dispose();
            }

            this.ChartControl.YAxisRangeTypeRight = YAxisRangeType.Automatic;
            #endregion

            #region Term BarTimer
            _textFont2.Dispose();
            _textFont.Dispose();
            _textBrush.Dispose();
            _stringFormat.Dispose();

            _updateTimer.Tick -= new EventHandler(_OnUpdateTimerTick);
            _updateTimer.Dispose();

            if (_updateTimer != null)
            {
                _updateTimer.Enabled = false;
                _updateTimer = null;
            }
            #endregion BarTimer

            #region Right Panel

            ChartControl.Controls.RemoveByKey("main_tabcontrol");
            ChartControl.Controls.RemoveByKey("right_panel");
            _main_tabcontrol = null;
            _right_panel = null;         
            #endregion
        }

        public override void Dispose()
        {
            if (_buttonsloaded == true)
            {
                _strip.Items.Remove(_btn_rpanel);
                _strip.Items.Remove(_sepL);
                _strip.Items.Remove(_sepR);
            }
            base.Dispose();
        }

        #region Properties
        #region AutoScroll
        [Description("Scroll speed by scroll buttons. Select suitable to Chart and Instrument.")]
        [Category("Parameters")]
        [Gui.Design.DisplayName("Scroll Speed")]
        public int Speed
        {
            get { return _speed; }
            set { _speed = Math.Max(1, value); }
        }

        [Description("Select extra ticks to have on beyond maximum amd minimum dataseries prices.")]
        [Category("Parameters")]
        [Gui.Design.DisplayName("Extra Ticks")]
        public int Extra_margin
        {
            get { return _extra_margin; }
            set { _extra_margin = Math.Max(1, value); }
        }

        [Description("Select Center Margin Ticks for Center AutoScroll.")]
        [Category("Parameters")]
        [Gui.Design.DisplayName("Center AutoScroll Margin")]
        public int Center_margin_ticks
        {
            get { return _center_margin_ticks; }
            set { _center_margin_ticks = Math.Max(2, value); }
        }

        [Description("Show or Hide the Auto Scroll Button. Auto Scroll works only with show.")]
        [Category("Parameters")]
        [Gui.Design.DisplayName("Show AutoScroll Button")]
        public bool Show_as
        {
            get { return _show_as; }
            set { _show_as = value; }
        }

        [Description("Select Location of Auto Scroll Button on Chart")]
        [Category("Parameters")]
        [Gui.Design.DisplayName("Auto Scroll Button Position")]
        public AutoScrollButtonPosition Asbp
        {
            get { return _asbp; }
            set { _asbp = value; }
        }

        [Description("Select Auto Scroll Type. Extreem -> normal auto scroll from boundaries, Center -> keeps close of current bar in center.")]
        [Category("Parameters")]
        [Gui.Design.DisplayName("AutoScroll Type")]
        public AutoScrollType Ast
        {
            get { return _ast; }
            set { _ast = value; }
        }

        [Description("Show or Hide Vertical Scroll Bar.")]
        [Category("Parameters")]
        [Gui.Design.DisplayName("Show VerticalScroll Bar")]
        public bool Show_vs
        {
            get { return _show_vs; }
            set { _show_vs = value; }
        }

        [Description("Show or Hide Vertical Drag Button.")]
        [Category("Parameters")]
        [Gui.Design.DisplayName("Show VerticalDrag Button")]
        public bool Show_vd
        {
            get { return _show_vd; }
            set { _show_vd = value; }
        }
        #endregion
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.
// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    public partial class Indicator : IndicatorBase
    {
        private GTools[] cacheGTools = null;

        private static GTools checkGTools = new GTools();

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public GTools GTools(AutoScrollButtonPosition asbp, AutoScrollType ast, int center_margin_ticks, int extra_margin, bool show_as, bool show_vd, bool show_vs, int speed)
        {
            return GTools(Input, asbp, ast, center_margin_ticks, extra_margin, show_as, show_vd, show_vs, speed);
        }

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public GTools GTools(Data.IDataSeries input, AutoScrollButtonPosition asbp, AutoScrollType ast, int center_margin_ticks, int extra_margin, bool show_as, bool show_vd, bool show_vs, int speed)
        {
            if (cacheGTools != null)
                for (int idx = 0; idx < cacheGTools.Length; idx++)
                    if (cacheGTools[idx].Asbp == asbp && cacheGTools[idx].Ast == ast && cacheGTools[idx].Center_margin_ticks == center_margin_ticks && cacheGTools[idx].Extra_margin == extra_margin && cacheGTools[idx].Show_as == show_as && cacheGTools[idx].Show_vd == show_vd && cacheGTools[idx].Show_vs == show_vs && cacheGTools[idx].Speed == speed && cacheGTools[idx].EqualsInput(input))
                        return cacheGTools[idx];

            lock (checkGTools)
            {
                checkGTools.Asbp = asbp;
                asbp = checkGTools.Asbp;
                checkGTools.Ast = ast;
                ast = checkGTools.Ast;
                checkGTools.Center_margin_ticks = center_margin_ticks;
                center_margin_ticks = checkGTools.Center_margin_ticks;
                checkGTools.Extra_margin = extra_margin;
                extra_margin = checkGTools.Extra_margin;
                checkGTools.Show_as = show_as;
                show_as = checkGTools.Show_as;
                checkGTools.Show_vd = show_vd;
                show_vd = checkGTools.Show_vd;
                checkGTools.Show_vs = show_vs;
                show_vs = checkGTools.Show_vs;
                checkGTools.Speed = speed;
                speed = checkGTools.Speed;

                if (cacheGTools != null)
                    for (int idx = 0; idx < cacheGTools.Length; idx++)
                        if (cacheGTools[idx].Asbp == asbp && cacheGTools[idx].Ast == ast && cacheGTools[idx].Center_margin_ticks == center_margin_ticks && cacheGTools[idx].Extra_margin == extra_margin && cacheGTools[idx].Show_as == show_as && cacheGTools[idx].Show_vd == show_vd && cacheGTools[idx].Show_vs == show_vs && cacheGTools[idx].Speed == speed && cacheGTools[idx].EqualsInput(input))
                            return cacheGTools[idx];

                GTools indicator = new GTools();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.Asbp = asbp;
                indicator.Ast = ast;
                indicator.Center_margin_ticks = center_margin_ticks;
                indicator.Extra_margin = extra_margin;
                indicator.Show_as = show_as;
                indicator.Show_vd = show_vd;
                indicator.Show_vs = show_vs;
                indicator.Speed = speed;
                Indicators.Add(indicator);
                indicator.SetUp();

                GTools[] tmp = new GTools[cacheGTools == null ? 1 : cacheGTools.Length + 1];
                if (cacheGTools != null)
                    cacheGTools.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cacheGTools = tmp;
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
        public Indicator.GTools GTools(AutoScrollButtonPosition asbp, AutoScrollType ast, int center_margin_ticks, int extra_margin, bool show_as, bool show_vd, bool show_vs, int speed)
        {
            return _indicator.GTools(Input, asbp, ast, center_margin_ticks, extra_margin, show_as, show_vd, show_vs, speed);
        }

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public Indicator.GTools GTools(Data.IDataSeries input, AutoScrollButtonPosition asbp, AutoScrollType ast, int center_margin_ticks, int extra_margin, bool show_as, bool show_vd, bool show_vs, int speed)
        {
            return _indicator.GTools(input, asbp, ast, center_margin_ticks, extra_margin, show_as, show_vd, show_vs, speed);
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
        public Indicator.GTools GTools(AutoScrollButtonPosition asbp, AutoScrollType ast, int center_margin_ticks, int extra_margin, bool show_as, bool show_vd, bool show_vs, int speed)
        {
            return _indicator.GTools(Input, asbp, ast, center_margin_ticks, extra_margin, show_as, show_vd, show_vs, speed);
        }

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public Indicator.GTools GTools(Data.IDataSeries input, AutoScrollButtonPosition asbp, AutoScrollType ast, int center_margin_ticks, int extra_margin, bool show_as, bool show_vd, bool show_vs, int speed)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.GTools(input, asbp, ast, center_margin_ticks, extra_margin, show_as, show_vd, show_vs, speed);
        }
    }
}
#endregion
