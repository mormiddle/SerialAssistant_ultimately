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
            chart1.Series["standard"].Points.AddXY(0, 0);
            chart1.Series["standard"].Points.AddXY(60, 0);
        }

        #region 波形运行
        List<double> listData = new List<double>();
        double autoMove = 0, interval = 0, move = 0;
        public void Run()
        {

            while (true)
            {
                Thread.Sleep(500);
                Random random = new Random();
                int temp = random.Next(10, 1000);
                double tempD = 10 + (temp / (double)1000.0);
                listData.Add(tempD);
                try
                {
                    DisplayChart(listData, chart1.Series["Err"], label1, ref autoMove, move, ref interval, false);
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
        private void DisplayChart(List<double> listErr, Series series, Control controlInterval, ref double autoMove, double move, ref double interval, bool isAuto)
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
                    controlInterval.Text = "每大格值:" + tempInderval;
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
        private void btnStart1_Click(object sender, EventArgs e)
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
        private void btnStop1_Click(object sender, EventArgs e)
        {
            if (th != null && th.IsAlive)
            {
                th.Abort();
            }
        }

        #endregion

        #region 自适应开关
        private void button1_Click(object sender, EventArgs e)
        {
            move = 0;
            DisplayChart(listData, chart1.Series["Err"], label1, ref autoMove, move, ref interval, true);
        }
        #endregion
    }
}
