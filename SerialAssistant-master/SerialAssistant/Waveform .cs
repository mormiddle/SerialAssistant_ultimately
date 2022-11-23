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
        List<double> Passage1_real = new List<double>();
        List<double> Passage2_real = new List<double>();
        List<double> Passage3_real = new List<double>();
        List<double> Passage4_real = new List<double>();
        List<double> Passage5_real = new List<double>();
        List<double> Passage6_real = new List<double>();
        List<double> Passage7_real = new List<double>();
        List<double> Passage8_real = new List<double>();
        List<double> Passage9_real = new List<double>();
        List<double> Passage10_real = new List<double>();
        List<double> Passage1_lmag = new List<double>();
        List<double> Passage2_lmag = new List<double>();
        List<double> Passage3_lmag = new List<double>();
        List<double> Passage4_lmag = new List<double>();
        List<double> Passage5_lmag = new List<double>();
        List<double> Passage6_lmag = new List<double>();
        List<double> Passage7_lmag = new List<double>();
        List<double> Passage8_lmag = new List<double>();
        List<double> Passage9_lmag = new List<double>();
        List<double> Passage10_lmag = new List<double>();


        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            chart_real.Series["Series1"].Points.AddXY(50, 50);
            


        }

        #region 波形运行
        List<double> listData_real = new List<double>();
    
        double autoMove = 0, interval = 0, move = 0;
        public void Run()
        {

            while (true)
            {
                Thread.Sleep(10);
                Random random = new Random();
                int temp = random.Next(10, 1000);
                double tempD = 10 + (temp / (double)1.0);
                //listData.Add(tempD);
                listData_real.Add(tempD);

                try
                {
                    DisplayChart(Passage1_real, chart_real.Series["Series1"], ref autoMove, 0, ref interval, false);
                    DisplayChart(Passage2_real, chart_real.Series["Series2"], ref autoMove, 100, ref interval, false);
                    DisplayChart(Passage3_real, chart_real.Series["Series3"], ref autoMove, 200, ref interval, false);
                    DisplayChart(Passage4_real, chart_real.Series["Series4"], ref autoMove, 300, ref interval, false);
                    DisplayChart(Passage5_real, chart_real.Series["Series5"], ref autoMove, 400, ref interval, false);
                    DisplayChart(Passage6_real, chart_real.Series["Series6"], ref autoMove, 500, ref interval, false);
                    DisplayChart(Passage7_real, chart_real.Series["Series7"], ref autoMove, 600, ref interval, false);
                    DisplayChart(Passage8_real, chart_real.Series["Series8"], ref autoMove, 700, ref interval, false);
                    DisplayChart(Passage9_real, chart_real.Series["Series9"], ref autoMove, 800, ref interval, false);
                    DisplayChart(Passage10_real, chart_real.Series["Series10"], ref autoMove, 900, ref interval, false);


                }
                catch (Exception ex)
                {
                    string s = ex.Message;
                }
            }
        }
        #endregion


        /// <summary>
        /// 数据位处理函数
        /// </summary>
        /// <param name="showbuffer"></param>
        public void ReceiveBataDispose(byte[] dyteList)
        {

            List<double> InputList = new List<double>(); //先将byte转为string类型
            foreach (double temp in dyteList)
            {
                InputList.Add(temp);
            }

            //对数据进行划分一共40个字节，10个通道的数据，4个字节为一个通道，对应实部高位，实部低位，虚部高位，虚部低位

            while (true)
            {
                Passage1_real.Add(InputList[0] * 256 + InputList[1]);
                Passage1_lmag.Add(InputList[2] * 256 + InputList[3]);
                Passage2_real.Add(InputList[4] * 256 + InputList[5]);
                Passage2_lmag.Add(InputList[6] * 256 + InputList[7]);
                Passage3_real.Add(InputList[8] * 256 + InputList[9]);
                Passage3_lmag.Add(InputList[10] * 256 + InputList[11]);
                Passage4_real.Add(InputList[12] * 256 + InputList[13]);
                Passage4_lmag.Add(InputList[14] * 256 + InputList[15]);
                Passage5_real.Add(InputList[16] * 256 + InputList[17]);
                Passage5_lmag.Add(InputList[18] * 256 + InputList[19]);
                Passage6_real.Add(InputList[20] * 256 + InputList[21]);
                Passage6_lmag.Add(InputList[22] * 256 + InputList[23]);
                Passage7_real.Add(InputList[24] * 256 + InputList[25]);
                Passage7_lmag.Add(InputList[26] * 256 + InputList[27]);
                Passage8_real.Add(InputList[28] * 256 + InputList[29]);
                Passage8_lmag.Add(InputList[30] * 256 + InputList[31]);
                Passage9_real.Add(InputList[32] * 256 + InputList[33]);
                Passage9_lmag.Add(InputList[34] * 256 + InputList[35]);
                Passage10_real.Add(InputList[36] * 256 + InputList[37]);
                Passage10_lmag.Add(InputList[38] * 256 + InputList[39]);
            }
            
        }




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
