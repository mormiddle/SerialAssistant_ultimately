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
        public List<double> SerialPortData = new List<double>(); //用于存储串口的数据
        List<double> Passage1 = new List<double>();
        List<double> Passage2 = new List<double>();
        List<double> Passage3 = new List<double>();
        List<double> Passage4 = new List<double>();
        List<double> Passage5 = new List<double>();
        List<double> Passage6 = new List<double>();
        List<double> Passage7 = new List<double>();
        List<double> Passage8 = new List<double>();
        List<double> Passage9 = new List<double>();
        List<double> Passage10 = new List<double>();



        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {

          


        }

    

        /// <summary>
        /// 数据位处理函数
        /// </summary>
        /// <param name="showbuffer"></param>
        public void ReceiveBataDispose(List<double> InputList)
        {

            //对数据进行划分一共40个字节，10个通道的数据，4个字节为一个通道，对应实部高位，实部低位，虚部高位，虚部低位


            while (SerialPortData.Count != 0)
            {
                if (Passage1.Count > 1000000 | Passage2.Count > 1000000 | Passage3.Count > 1000000 | Passage4.Count > 1000000 | Passage5.Count > 1000000 |
                    Passage6.Count > 1000000 | Passage7.Count > 1000000 | Passage8.Count > 1000000 | Passage9.Count > 1000000 | Passage10.Count > 1000000)
                {
                    Passage1.Clear();
                    Passage2.Clear();
                    Passage3.Clear();
                    Passage4.Clear();
                    Passage5.Clear();
                    Passage6.Clear();
                    Passage7.Clear();
                    Passage8.Clear();
                    Passage9.Clear();
                    Passage10.Clear();
                }

                Passage1.Add(InputList[0] * 256 + InputList[1]);
                Passage1.Add(InputList[2] * 256 + InputList[3]);
                Passage2.Add(InputList[4] * 256 + InputList[5]);
                Passage2.Add(InputList[6] * 256 + InputList[7]);
                Passage3.Add(InputList[8] * 256 + InputList[9]);
                Passage3.Add(InputList[10] * 256 + InputList[11]);
                Passage4.Add(InputList[12] * 256 + InputList[13]);
                Passage4.Add(InputList[14] * 256 + InputList[15]);
                Passage5.Add(InputList[16] * 256 + InputList[17]);
                Passage5.Add(InputList[18] * 256 + InputList[19]);
                Passage6.Add(InputList[20] * 256 + InputList[21]);
                Passage6.Add(InputList[22] * 256 + InputList[23]);
                Passage7.Add(InputList[24] * 256 + InputList[25]);
                Passage7.Add(InputList[26] * 256 + InputList[27]);
                Passage8.Add(InputList[28] * 256 + InputList[29]);
                Passage8.Add(InputList[30] * 256 + InputList[31]);
                Passage9.Add(InputList[32] * 256 + InputList[33]);
                Passage9.Add(InputList[34] * 256 + InputList[35]);
                Passage10.Add(InputList[36] * 256 + InputList[37]);
                Passage10.Add(InputList[38] * 256 + InputList[39]);
                if (SerialPortData.Count >= 1000000)
                {
                    SerialPortData.Clear();
                }
            }


        }





        #region 窗口关闭
        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            MainForm.intimewindowIsOpen = false;
        }
        #endregion

        Thread th;

        private void btnStart_Click(object sender, EventArgs e)
        {
           
            th = new Thread(GetData);
            Random r = new Random();
            th.IsBackground = true;
            th.Start();
            if (Passage1.Count != 0)
            {
                for (int j = 0; j < 10; j++)
                {
                    for (int i = 0; i < 1600; i++)
                    {
                        this.chart_lmag.Series[j].Points.AddXY((i + 1), Passage1[i] + j);
                        //this.chart_real.Series[j].Points.AddXY((i + 1), r.Next(j, j + 2) + j);
                    }
                }
            }
            
        }

    

        #region 波形停止开关
        private void btnStop_Click(object sender, EventArgs e)
        {
            
        }

        private void GetData()
        {
            ReceiveBataDispose(SerialPortData);
        }

        #endregion

    }
}
