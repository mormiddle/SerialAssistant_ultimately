using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace SerialAssistant
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            chart1_real.Series[0].Points.AddXY(50, 50);
            chart2_real.Series[0].Points.AddXY(50, 50);
            chart3_real.Series[0].Points.AddXY(50, 50);
            chart4_real.Series[0].Points.AddXY(50, 50);
            chart5_real.Series[0].Points.AddXY(50, 50);
            chart6_real.Series[0].Points.AddXY(50, 50);
            chart7_real.Series[0].Points.AddXY(50, 50);
            chart8_real.Series[0].Points.AddXY(50, 50);
            chart9_real.Series[0].Points.AddXY(50, 50);
            chart10_real.Series[0].Points.AddXY(50, 50);
            chart1_lmag.Series[0].Points.AddXY(50, 50);
            chart2_lmag.Series[0].Points.AddXY(50, 50);
            chart3_lmag.Series[0].Points.AddXY(50, 50);
            chart4_lmag.Series[0].Points.AddXY(50, 50);
            chart5_lmag.Series[0].Points.AddXY(50, 50);
            chart6_lmag.Series[0].Points.AddXY(50, 50);
            chart7_lmag.Series[0].Points.AddXY(50, 50);
            chart8_lmag.Series[0].Points.AddXY(50, 50);
            chart9_lmag.Series[0].Points.AddXY(50, 50);
            chart10_lmag.Series[0].Points.AddXY(50, 50);

        }

        #region 波形运行
        List<double> listData1_real = new List<double>();
        List<double> listData2_real = new List<double>();
        List<double> listData3_real = new List<double>();
        List<double> listData4_real = new List<double>();
        List<double> listData5_real = new List<double>();
        List<double> listData6_real = new List<double>();
        List<double> listData7_real = new List<double>();
        List<double> listData8_real = new List<double>();
        List<double> listData9_real = new List<double>();
        List<double> listData10_real = new List<double>();

        List<double> listData1_lmag = new List<double>();
        List<double> listData2_lmag = new List<double>();
        List<double> listData3_lmag = new List<double>();
        List<double> listData4_lmag = new List<double>();
        List<double> listData5_lmag = new List<double>();
        List<double> listData6_lmag = new List<double>();
        List<double> listData7_lmag = new List<double>();
        List<double> listData8_lmag = new List<double>();
        List<double> listData9_lmag = new List<double>();
        List<double> listData10_lmag = new List<double>();


        double autoMove = 0, interval = 0, move = 0;
        public void Run()
        {

            while (true)
            {
                Thread.Sleep(50);
                Random random = new Random();
                int temp = random.Next(10, 100);
                double tempD = 10 + (temp / (double)1.0);
                //listData.Add(tempD);
                try
                {
                    DisplayChart(listData1_real, chart1_real.Series["Err"], ref autoMove, move, ref interval, false);
                    DisplayChart(listData2_real, chart2_real.Series["Err"], ref autoMove, move, ref interval, false);
                    DisplayChart(listData3_real, chart3_real.Series["Err"], ref autoMove, move, ref interval, false);
                    DisplayChart(listData4_real, chart4_real.Series["Err"], ref autoMove, move, ref interval, false);
                    DisplayChart(listData5_real, chart5_real.Series["Err"], ref autoMove, move, ref interval, false);
                    DisplayChart(listData6_real, chart6_real.Series["Err"], ref autoMove, move, ref interval, false);
                    DisplayChart(listData7_real, chart7_real.Series["Err"], ref autoMove, move, ref interval, false);
                    DisplayChart(listData8_real, chart8_real.Series["Err"], ref autoMove, move, ref interval, false);
                    DisplayChart(listData9_real, chart9_real.Series["Err"], ref autoMove, move, ref interval, false);
                    DisplayChart(listData10_real, chart10_real.Series["Err"], ref autoMove, move, ref interval, false);

                    DisplayChart(listData1_lmag, chart1_lmag.Series["Err"], ref autoMove, move, ref interval, false);
                    DisplayChart(listData2_lmag, chart2_lmag.Series["Err"], ref autoMove, move, ref interval, false);
                    DisplayChart(listData3_lmag, chart3_lmag.Series["Err"], ref autoMove, move, ref interval, false);
                    DisplayChart(listData4_lmag, chart4_lmag.Series["Err"], ref autoMove, move, ref interval, false);
                    DisplayChart(listData5_lmag, chart5_lmag.Series["Err"], ref autoMove, move, ref interval, false);
                    DisplayChart(listData6_lmag, chart6_lmag.Series["Err"], ref autoMove, move, ref interval, false);
                    DisplayChart(listData7_lmag, chart7_lmag.Series["Err"], ref autoMove, move, ref interval, false);
                    DisplayChart(listData8_lmag, chart8_lmag.Series["Err"], ref autoMove, move, ref interval, false);
                    DisplayChart(listData9_lmag, chart9_lmag.Series["Err"], ref autoMove, move, ref interval, false);
                    DisplayChart(listData10_lmag, chart10_lmag.Series["Err"], ref autoMove, move, ref interval, false);
                }
                catch (Exception ex)
                {
                    string s = ex.Message;
                }
            }
        }
        #endregion

        /// <summary>
        /// Chart绘图
        /// </summary>
        /// <param name="listErr">误差集合</param>
        /// <param name="series"></param>
        /// <param name="controlInterval">每大格值控件</param>
        /// <param name="autoMove">自动移动大小</param>
        /// <param name="move">移动大小</param>
        /// <param name="interval">每大格间距</param>
        /// <param name="isAuto">是否自动适应</param>
        private void DisplayChart(List<double> listErr, Series series, ref double autoMove, double move, ref double interval, bool isAuto)
        {
            while (listErr.Count > 61)
            {
                listErr.RemoveAt(0);
            }
            if (series.Points != null && series.Points.Count > 0)
            {
                this.BeginInvoke((EventHandler)(delegate { series.Points.Clear(); }));
            }
            if (listErr != null && listErr.Count > 1)
            {
                if (listErr.Count < 10 || isAuto)
                {
                    double max = int.MinValue;
                    double min = int.MaxValue;
                    foreach (double f in listErr)
                    {
                        max = max > f ? max : f;
                        min = min < f ? min : f;
                    }

                    interval = GetInterval(Math.Abs(max - min) / 2);

                    if (interval == 0)
                    {
                        interval = 1;
                    }

                    autoMove = ((max - min) / 2) + min;
                }


                double tempY = 0;
                double tempAutoMove = autoMove;
                double tempInderval = interval;
                this.BeginInvoke((EventHandler)(delegate
                {
                    for (int i = 0; i < listErr.Count; i++)
                    {
                        tempY = (listErr[i] - tempAutoMove) * (10 / tempInderval) + move;
                        series.Points.AddXY(i, tempY);
                    }
                }));

            }
        }

        #region 根据数据大小自适应
        private double GetInterval(double source)
        {
            double temp = source;
            if (source < 1)
            {
                string s = source.ToString("f10");
                string temps = s.Trim(new char[] { '0', '.' }).Length == 0 ? "0" : s.Trim(new char[] { '0', '.' });
                int index = s.IndexOf(temps);
                if (temps.Length > 1)
                {
                    temps = temps.Substring(0, 1);
                    int result = int.Parse(temps);
                    if (result < 5)
                    {
                        result = 5;
                        temps = s.Substring(0, index) + (result);
                        temp = double.Parse(temps);
                    }
                    else
                    {
                        result = 1;
                        temps = s.Substring(0, index) + (result);
                        temp = double.Parse(temps) * 10;
                    }

                }
                else
                {
                    int result = int.Parse(temps);
                    if (result > 5)
                    {
                        result = 1;
                        temps = s.Substring(0, index) + (result);
                        temp = double.Parse(temps) * 10;
                    }
                    else
                    {
                        result = 5;
                        temps = s.Substring(0, index) + (result);
                        temp = double.Parse(temps);
                    }
                }
            }
            else if (source < 10)
            {
                temp = Math.Ceiling(source);
            }
            else
            {
                temp = Math.Ceiling(temp / 10) * 10;
            }
            return temp;
        }
        #endregion

        #region 波形启动开关
        Thread th;
        private void btnStart_Click(object sender, EventArgs e)
        {
            if (th == null || !th.IsAlive)
            {
                th = new Thread(Run);
                th.IsBackground = true;
                th.Start();
            }
        }

        #endregion

        #region 波形停止开关
        private void btnStop_Click(object sender, EventArgs e)
        {
            if (th != null && th.IsAlive)
            {
                th.Abort();
            }
        }
        #endregion

    }
}
