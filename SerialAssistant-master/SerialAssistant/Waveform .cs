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
        //定义十个通道的实部和虚部列表
        public List<byte> SerialPortData = new List<byte>(); //用于存储串口的数据
        Thread th;



        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            this.chart_real.Series[0].Points.AddXY(50, 50);
            this.chart_lmag.Series[0].Points.AddXY(50, 50);


        }


        private async void ThreadRead()
        {
            System.Threading.Thread.Sleep(1000);
            Invoke(new Action(() =>
            {
                while (SerialPortData.Count > 41)
                {
                   
                    for (int i = 0; i < SerialPortData.Count - 41; i++)
                    {
                        System.Threading.Thread.Sleep(1000);
                        this.chart_real.Series[0].Points.AddXY(i, SerialPortData[i] * 256 + SerialPortData[i + 1]);
                        this.chart_lmag.Series[0].Points.AddXY(i, SerialPortData[i + 2] * 256 + SerialPortData[i + 3]);
                        /* this.chart_real.Series[1].Points.AddXY(i, SerialPortData[i + 4] * 256 + SerialPortData[i + 5]);
                         this.chart_lmag.Series[1].Points.AddXY(i, SerialPortData[i + 6] * 256 + SerialPortData[i + 7]);
                         this.chart_real.Series[2].Points.AddXY(i, SerialPortData[i + 8] * 256 + SerialPortData[i + 9]);
                         this.chart_lmag.Series[2].Points.AddXY(i, SerialPortData[i + 10] * 256 + SerialPortData[i + 11]);
                         this.chart_real.Series[3].Points.AddXY(i, SerialPortData[i + 8] * 256 + SerialPortData[i + 9]);
                         this.chart_lmag.Series[3].Points.AddXY(i, SerialPortData[i + 10] * 256 + SerialPortData[i + 11]);
                         this.chart_real.Series[4].Points.AddXY(i, SerialPortData[i + 8] * 256 + SerialPortData[i + 9]);
                         this.chart_lmag.Series[4].Points.AddXY(i, SerialPortData[i + 10] * 256 + SerialPortData[i + 11]);
                         this.chart_real.Series[5].Points.AddXY(i, SerialPortData[i + 8] * 256 + SerialPortData[i + 9]);
                         this.chart_lmag.Series[5].Points.AddXY(i, SerialPortData[i + 10] * 256 + SerialPortData[i + 11]);
                         this.chart_real.Series[6].Points.AddXY(i, SerialPortData[i + 8] * 256 + SerialPortData[i + 9]);
                         this.chart_lmag.Series[6].Points.AddXY(i, SerialPortData[i + 10] * 256 + SerialPortData[i + 11]);
                         this.chart_real.Series[7].Points.AddXY(i, SerialPortData[i + 8] * 256 + SerialPortData[i + 9]);
                         this.chart_lmag.Series[7].Points.AddXY(i, SerialPortData[i + 10] * 256 + SerialPortData[i + 11]);
                         this.chart_real.Series[8].Points.AddXY(i, SerialPortData[i + 8] * 256 + SerialPortData[i + 9]);
                         this.chart_lmag.Series[8].Points.AddXY(i, SerialPortData[i + 10] * 256 + SerialPortData[i + 11]);
                         this.chart_real.Series[9].Points.AddXY(i, SerialPortData[i + 8] * 256 + SerialPortData[i + 9]);
                         this.chart_lmag.Series[9].Points.AddXY(i, SerialPortData[i + 10] * 256 + SerialPortData[i + 11]);*/

                    }
                   
                }
            }));
        }

        List<double> listData = new List<double>();
        double autoMove = 0, interval = 0, move = 0;
        public void Run()
        {

            while (true)
            {
                Thread.Sleep(1000);
                try
                {
                    DisplayChart(listData, chart_real.Series[0], ref autoMove, move, ref interval, false);
                }
                catch (Exception ex)
                {
                    string s = ex.Message;
                }
            }
        }




        #region 窗口关闭
        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            MainForm.intimewindowIsOpen = false;
        }
        #endregion



        private void btnStart_Click(object sender, EventArgs e)
        {

            if (th == null || !th.IsAlive)
            {
                th = new Thread(Run);
                th.IsBackground = true;
                th.Start();
            }
        }
        


        #region 波形停止开关
        private void btnStop_Click(object sender, EventArgs e)
        {
            if (th != null && th.IsAlive)
            {
                th.Abort();
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

                this.BeginInvoke((EventHandler)(delegate
                {

                    for (int i = 0; i < listErr.Count; i++)
                    {
                        tempY = listErr[i] + move;
                        series.Points.AddXY(i, tempY);
                    }
                }));

            }
        }


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


    }
}
