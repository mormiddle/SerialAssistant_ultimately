using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Windows.Forms.DataVisualization.Charting;

namespace SerialAssistant
{
    public partial class MainForm : Form
    {
        private long receive_count = 0; //接收字节计数
        private StringBuilder sb = new StringBuilder();    //为了避免在接收处理函数中反复调用，依然声明为一个全局变量
        private DateTime current_time = new DateTime();    //为了避免在接收处理函数中反复调用，依然声明为一个全局变量
        private bool is_need_time = true;
        private List<byte> buffer = new List<byte>(); //设置缓存处理CRC32串口的校验
        public static bool intimewindowIsOpen = false; //判断波形窗口是否创建
        List<byte> CheckedData = new List<byte>();//申请一个大容量的数组
        //private List<byte> SerialPortReceiveData = new List<byte>(); //用于存储串口的数据
        Thread th;
        DateTime timeStart = new DateTime();//采集开始时间
        int pointIndex = 0;
        int[] Serialport1XyAutoSet = new int[20] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        private Queue<int> Freq1RealDataQueue = new Queue<int>(100);//以下两行为波形显示做准备
        private Queue<int> Freq1ImagDataQueue = new Queue<int>(100);//以下两行为波形显示做准备
        private Queue<int> Freq2RealDataQueue = new Queue<int>(100);//以下两行为波形显示做准备
        private Queue<int> Freq2ImagDataQueue = new Queue<int>(100);//以下两行为波形显示做准备
        private Queue<int> Freq3RealDataQueue = new Queue<int>(100);//以下两行为波形显示做准备
        private Queue<int> Freq3ImagDataQueue = new Queue<int>(100);//以下两行为波形显示做准备
        private Queue<int> Freq4RealDataQueue = new Queue<int>(100);//以下两行为波形显示做准备
        private Queue<int> Freq4ImagDataQueue = new Queue<int>(100);//以下两行为波形显示做准备
        private Queue<int> Freq5RealDataQueue = new Queue<int>(100);//以下两行为波形显示做准备
        private Queue<int> Freq5ImagDataQueue = new Queue<int>(100);//以下两行为波形显示做准备
        private Queue<int> Freq6RealDataQueue = new Queue<int>(100);//以下两行为波形显示做准备
        private Queue<int> Freq6ImagDataQueue = new Queue<int>(100);//以下两行为波形显示做准备
        private Queue<int> Freq7RealDataQueue = new Queue<int>(100);//以下两行为波形显示做准备
        private Queue<int> Freq7ImagDataQueue = new Queue<int>(100);//以下两行为波形显示做准备
        private Queue<int> Freq8RealDataQueue = new Queue<int>(100);//以下两行为波形显示做准备
        private Queue<int> Freq8ImagDataQueue = new Queue<int>(100);//以下两行为波形显示做准备
        private Queue<int> Freq9RealDataQueue = new Queue<int>(100);//以下两行为波形显示做准备
        private Queue<int> Freq9ImagDataQueue = new Queue<int>(100);//以下两行为波形显示做准备
        private Queue<int> Freq10RealDataQueue = new Queue<int>(100);//以下两行为波形显示做准备
        private Queue<int> Freq10ImagDataQueue = new Queue<int>(100);//以下两行为波形显示做准备
        int start = 0;//充当指针的作用



        public MainForm()
        {
            InitializeComponent();
        }

        private bool search_port_is_exist(String item, String[] port_list)
        {
            for (int i = 0; i < port_list.Length; i++)
            {
                if (port_list[i].Equals(item))
                {
                    return true;
                }
            }

            return false;
        }

        #region 扫描串口列表并添加到选择框
        private void Update_Serial_List()
        {
            try
            {
                /* 搜索串口 */
                String[] cur_port_list = System.IO.Ports.SerialPort.GetPortNames();

                /* 刷新串口列表comboBox */
                int count = comboBox1.Items.Count;
                if (count == 0)
                {
                    //combox中无内容，将当前串口列表全部加入
                    comboBox1.Items.AddRange(cur_port_list);
                    return;
                }
                else
                {
                    //combox中有内容

                    //判断有无新插入的串口
                    for (int i = 0; i < cur_port_list.Length; i++)
                    {
                        if (!comboBox1.Items.Contains(cur_port_list[i]))
                        {
                            //找到新插入串口，添加到combox中
                            comboBox1.Items.Add(cur_port_list[i]);
                        }
                    }

                    //判断有无拔掉的串口
                    for (int i = 0; i < count; i++)
                    {
                        if (!search_port_is_exist(comboBox1.Items[i].ToString(), cur_port_list))
                        {
                            //找到已被拔掉的串口，从combox中移除
                            comboBox1.Items.RemoveAt(i);
                        }
                    }
                }

                /* 如果当前选中项为空，则默认选择第一项 */
                if (comboBox1.Items.Count > 0)
                {
                    if (comboBox1.Text.Equals(""))
                    {
                        //软件刚启动时，列表项的文本值为空
                        comboBox1.Text = comboBox1.Items[0].ToString();
                       
                    }
                }
                else
                {
                    //无可用列表，清空文本值
                    comboBox1.Text = "";
                }


            }
            catch (Exception)
            {
                //当下拉框被打开时，修改下拉框会发生异常
                return;
            }
        }
        #endregion


        private void Form1_Load(object sender, EventArgs e)
        {
            /* 添加串口选择列表 */
            Update_Serial_List();

            /* 添加波特率列表 */
            string[] baud = { "9600", "38400", "57600", "115200" };
            comboBox2.Items.AddRange(baud);

            /* 添加数据位列表 */
            string[] data_length = { "5", "6", "7", "8", "9" };
            comboBox3.Items.AddRange(data_length);

            /* 添加校验位列表 */
            string[] verification_mode = { "None", "Odd", "Even", "Mark", "Space" };
            comboBox4.Items.AddRange(verification_mode);

            /* 添加停止位列表 */
            string[] stop_length = { "1", "1.5", "2" };
            comboBox5.Items.AddRange(stop_length);

            /* 设置默认选择值 */
            comboBox2.Text = "115200";
            comboBox3.Text = "8";
            comboBox4.Text = "None";
            comboBox5.Text = "1";

            /* 在串口未打开的情况下每隔1s刷新一次串口列表框 */
            timer1.Interval = 1000;
            timer1.Start();

            //初始化波形显示队列
            for (int cache1 = 0; cache1 < 100; cache1++)
            {
                UpdateSerialport1DataQueueValue(0, 0, 0, 0);
                UpdateSerialport2DataQueueValue(0, 0, 0, 0);
                UpdateSerialport3DataQueueValue(0, 0, 0, 0);
                UpdateSerialport4DataQueueValue(0, 0, 0, 0);
                UpdateSerialport5DataQueueValue(0, 0, 0, 0);

            }

        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            Update_Serial_List();

        }

        #region 右上角串口连接开关
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                //将可能产生异常的代码放置在try块中
                //根据当前串口属性来判断是否打开
                if (serialPort1.IsOpen)
                {
                    //串口已经处于打开状态

                    serialPort1.Close();    //关闭串口
                    button1.BackgroundImage = global::SerialAssistant.Properties.Resources.connect;
                    comboBox1.Enabled = true;
                    comboBox2.Enabled = true;
                    comboBox3.Enabled = true;
                    comboBox4.Enabled = true;
                    comboBox5.Enabled = true;
                    label6.Text = "串口已关闭!";
                    label6.ForeColor = Color.Red;
                    //button5.Enabled = false;        //失能发送按钮
                    checkBox4.Enabled = false;


                    //开启端口扫描
                    timer1.Interval = 1000;
                    timer1.Start();
                }
                else
                {
                    /* 串口已经处于关闭状态，则设置好串口属性后打开 */
                    //停止串口扫描
                    timer1.Stop();

                    comboBox1.Enabled = false;
                    comboBox2.Enabled = false;
                    comboBox3.Enabled = false;
                    comboBox4.Enabled = false;
                    comboBox5.Enabled = false;
                    checkBox4.Enabled = true;
                    serialPort1.PortName = comboBox1.Text;
                    serialPort1.BaudRate = Convert.ToInt32(comboBox2.Text);
                    serialPort1.DataBits = Convert.ToInt16(comboBox3.Text);

                    if (comboBox4.Text.Equals("None"))
                        serialPort1.Parity = System.IO.Ports.Parity.None;
                    else if (comboBox4.Text.Equals("Odd"))
                        serialPort1.Parity = System.IO.Ports.Parity.Odd;
                    else if (comboBox4.Text.Equals("Even"))
                        serialPort1.Parity = System.IO.Ports.Parity.Even;
                    else if (comboBox4.Text.Equals("Mark"))
                        serialPort1.Parity = System.IO.Ports.Parity.Mark;
                    else if (comboBox4.Text.Equals("Space"))
                        serialPort1.Parity = System.IO.Ports.Parity.Space;

                    if (comboBox5.Text.Equals("1"))
                        serialPort1.StopBits = System.IO.Ports.StopBits.One;
                    else if (comboBox5.Text.Equals("1.5"))
                        serialPort1.StopBits = System.IO.Ports.StopBits.OnePointFive;
                    else if (comboBox5.Text.Equals("2"))
                        serialPort1.StopBits = System.IO.Ports.StopBits.Two;

                    //打开串口，设置状态
                    serialPort1.Open();
                    button1.BackgroundImage = global::SerialAssistant.Properties.Resources.disconnect;
                    label6.Text = "串口已打开!";
                    label6.ForeColor = Color.Green;

                    //使能发送按钮
                    //button5.Enabled = true;

                }
            }
            catch (Exception ex)
            {
                //捕获可能发生的异常并进行处理

                //捕获到异常，创建一个新的对象，之前的不可以再用  
                serialPort1 = new System.IO.Ports.SerialPort(components);
                serialPort1.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(serialPort1_DataReceived);

                //响铃并显示异常给用户
                System.Media.SystemSounds.Beep.Play();
                button1.BackgroundImage = global::SerialAssistant.Properties.Resources.connect;
                MessageBox.Show(ex.Message);
                comboBox1.Enabled = true;
                comboBox2.Enabled = true;
                comboBox3.Enabled = true;
                comboBox4.Enabled = true;
                comboBox5.Enabled = true;
                label6.Text = "串口已关闭!";
                label6.ForeColor = Color.Red;
                //button5.Enabled = false;        //失能发送按钮
                checkBox4.Enabled = false;

                //开启串口扫描
                timer1.Interval = 1000;
                timer1.Start();
            }
        }
        #endregion


        #region 串口接收数据
        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            /* 串口接收事件处理 */

            /* 刷新定时器 */
            if (checkBox3.Checked)
            {
                timer3.Interval = (int)numericUpDown2.Value;
            }


            int num = serialPort1.BytesToRead;      //获取接收缓冲区中的字节数
            if (num == 0)
            {
                return;
            }
            byte[] received_buf = new byte[num];    //声明一个大小为num的字节数据用于存放读出的byte型数据


            receive_count += num;                   //接收字节计数变量增加nun
            serialPort1.Read(received_buf, 0, num);   //读取接收缓冲区中num个字节到byte数组中

            #region 数据校验
            buffer.AddRange(received_buf); //缓存数据

            // resize arr
            int count = buffer.Count;

            while ( start + 44 <= count )
            {
                // head, tail
                if (buffer[start] != 0xAA || buffer[start +1] != 0x29 || buffer[start+43] != 0x80)
                {
                    start+=2;
                    continue;
                }

                // CRC8: from  start + 2  to start + 41, check by start + 42
                if ( CRC8(buffer, start+2, 40) != buffer[start +42] )
                {
                    start += 2;
                    continue;
                }

                // append data
                {
                    // copy 40 bytes from start
                    for (int i = start + 2; i < start + 42; i++)
                    {
                        CheckedData.Add(buffer[i]);
                    }

                   
                    //show bytes
                    ShowSerialPortReceive(buffer, start);
                }

                start += 44;
            }




            #endregion

        }
        #endregion



        #region 显示串口接收的数据
        private void ShowSerialPortReceive(List<byte> showbuffer, int start)
        {
            sb.Clear();     //防止出错,首先清空字符串构造器 //不使用这个，就会重复显示输入的数据

            //HEX模式显示
            for (int i = start; i < start + 44; i++)
            {
                sb.Append(buffer[i].ToString("X2") + ' ');//将byte型数据转化为2位16进制文本显示，并用空格隔开


            }

            try
            {
                //因为要访问UI资源，所以需要使用invoke方式同步ui
                Invoke((EventHandler)(delegate
                {
                    if (is_need_time && checkBox3.Checked)
                    {
                        /* 需要加时间戳 */
                        is_need_time = false;   //清空标志位
                        current_time = System.DateTime.Now;     //获取当前时间
                        textBox1.AppendText("\r\n[" + current_time.ToString("HH:mm:ss") + "]" + sb.ToString());
                    }
                    else
                    {
                        /* 不需要时间戳 */
                        textBox1.AppendText(sb.ToString());
                    }

                    label8.Text = "接收：" + receive_count.ToString() + " Bytes";
                }
                  )
                );
            }
            catch (Exception ex)
            {
                //响铃并显示异常给用户
                System.Media.SystemSounds.Beep.Play();
                MessageBox.Show(ex.Message);

            }
        }

        #endregion



        #region CRC8校验函数
        public static byte CRC8(List<byte> buffer, int start, int length)
        {
            byte crc = 0;// Initial value

            for (int j = start; j < start + length; j++)
            {
                crc ^= buffer[j];
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x80) != 0)
                    {
                        crc <<= 1;
                        crc ^= 0x07;
                    }
                    else
                    {
                        crc <<= 1;
                    }
                }
            }
            return  crc ;
        }




        #endregion



        #region 清除开关
        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
            receive_count = 0;          //接收计数清零
            send_count = 0;             //发送计数
            label8.Text = "接收：" + receive_count.ToString() + " Bytes";
            label7.Text = "发送：" + receive_count.ToString() + " Bytes";


            this.chart_real1.Series[0].Points.Clear();
            this.chart_lmag1.Series[0].Points.Clear();
            this.chart_real2.Series[0].Points.Clear();
            this.chart_lmag2.Series[0].Points.Clear();
            this.chart_real3.Series[0].Points.Clear();
            this.chart_lmag3.Series[0].Points.Clear();
            this.chart_real4.Series[0].Points.Clear();
            this.chart_lmag4.Series[0].Points.Clear();
            this.chart_real5.Series[0].Points.Clear();
            this.chart_lmag5.Series[0].Points.Clear();
            this.chart_real6.Series[0].Points.Clear();
            this.chart_lmag6.Series[0].Points.Clear();
            this.chart_real7.Series[0].Points.Clear();
            this.chart_lmag7.Series[0].Points.Clear();
            this.chart_real8.Series[0].Points.Clear();
            this.chart_lmag8.Series[0].Points.Clear();
            this.chart_real9.Series[0].Points.Clear();
            this.chart_lmag9.Series[0].Points.Clear();
            this.chart_real10.Series[0].Points.Clear();
            this.chart_lmag10.Series[0].Points.Clear();

        }
        #endregion



        #region 下载按钮
        private void button4_Click(object sender, EventArgs e)
        {
            if (th != null && th.IsAlive)
            {
                th.Abort();
            }
            /* 获取当前时间，用于填充文件名 */
            //eg.log_2021_05_08_10_13_31.txt          
            DateTime time = new DateTime();
            time = System.DateTime.Now;
            String fileName;
            String[] fileNames = new String[20];


            string foldPath;

            /* 获取当前接收区内容 */
            String recv_data = textBox1.Text;
            String[] DataStr = new String[20];
            for (int i = 0; i < 20; i++)
            {
                DataStr[i] = GetDataStr(CheckedData, i);
            }


            if (recv_data.Equals(""))
            {
                MessageBox.Show("接收数据为空，无需保存！");
                return;
            }

            

            /* 弹出文件夹选择框供用户选择 */
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择日志文件存储路径";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                foldPath = dialog.SelectedPath;
            }
            else
            {
                return;
            }
            TimeSpan span1 = time - timeStart;

            fileName = foldPath + "\\" + "log" + "_" + "时长" + span1.ToString(@"mm\.ss") + "_" + "日期" + "_" +time.ToString("yyyy_MM_dd_HH_mm_ss") + ".txt";
            for (int i = 0; i < 20; i++)
            {
                fileNames[i] = foldPath + "\\" + "log" + "_channel_" + i.ToString() + "_"+ ".txt";
            }



            try
            {
                /* 保存串口接收区的内容 */
                //创建 FileStream 类的实例
                FileStream fileStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileStream[] fileStreams = new FileStream[20];
                for (int i = 0; i < 20; i++)
                {
                    fileStreams[i] = new FileStream(fileNames[i], FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                }
              

                //将字符串转换为字节数组
                byte[] bytes = Encoding.UTF8.GetBytes(recv_data);
                byte[][] bytes_channels = new byte[20][];
                for (int i = 0; i < 20; i++)
                {
                    bytes_channels[i] = Encoding.UTF8.GetBytes(DataStr[i]);
                }



                //向文件中写入字节数组
                fileStream.Write(bytes, 0, bytes.Length);
                for (int i = 0; i < 20; i++)
                {
                    fileStreams[i].Write(bytes_channels[i], 0, bytes_channels[i].Length);
                }

                //刷新缓冲区
                fileStream.Flush();
                for (int i = 0; i < 20; i++)
                {
                    fileStreams[i].Flush();
                }

                //关闭流
                fileStream.Close();
                for (int i = 0; i < 20; i++)
                {
                    fileStreams[i].Close();
                }


                //提示用户
                MessageBox.Show("日志已保存!(" + fileName + ")");
                //ToMatlab(real1);
            }
            catch (Exception ex)
            {
                //提示用户
                MessageBox.Show("发生异常!(" + ex.ToString() + ")");
            }

        }
        #endregion


        #region 上传按钮
        private void button3_Click(object sender, EventArgs e)
        {
            if (th != null && th.IsAlive)
            {
                th.Abort();
            }


            double real1max = GetMax(CheckedData, 0);
            double real2max = GetMax(CheckedData, 2);
            double real3max = GetMax(CheckedData, 4);
            double real4max = GetMax(CheckedData, 6);
            double real5max = GetMax(CheckedData, 8);
            double real6max = GetMax(CheckedData, 10);
            double real7max = GetMax(CheckedData, 12);
            double real8max = GetMax(CheckedData, 14);
            double real9max = GetMax(CheckedData, 16);
            double real10max = GetMax(CheckedData, 18);

            MessageBox.Show("通道1:" + real1max.ToString() + "\n" + "通道2:" + real2max.ToString() + "\n" +
                            "通道3:" + real3max.ToString() + "\n" + "通道4:" + real4max.ToString() + "\n" +
                            "通道5:" + real5max.ToString() + "\n" + "通道6:" + real6max.ToString() + "\n" +
                            "通道7:" + real7max.ToString() + "\n" + "通道8:" + real8max.ToString() + "\n" +
                            "通道9:" + real9max.ToString() + "\n" + "通道10:" + real10max.ToString() + "\n");




        }
        #endregion

        private void button6_Click(object sender, EventArgs e)
        {
            //清空发送缓冲区
            //textBox2.Text = "";
        }

        private void comboBox7_SelectedIndexChanged(object sender, EventArgs e)
        {
            //清空发送缓冲区
            //textBox2.Text = comboBox7.SelectedItem.ToString();
        }

        private void panel10_Click(object sender, EventArgs e)
        {

        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            // button5_Click(button5, new EventArgs());    //调用发送按钮回调函数
            chart_real1.Series[0].Points.Clear();
            chart_real2.Series[0].Points.Clear();
            chart_real3.Series[0].Points.Clear();
            chart_real4.Series[0].Points.Clear();
            chart_real5.Series[0].Points.Clear();
            chart_real6.Series[0].Points.Clear();
            chart_real7.Series[0].Points.Clear();
            chart_real8.Series[0].Points.Clear();
            chart_real9.Series[0].Points.Clear();
            chart_real10.Series[0].Points.Clear();

            chart_lmag1.Series[0].Points.Clear();
            chart_lmag2.Series[0].Points.Clear();
            chart_lmag3.Series[0].Points.Clear();
            chart_lmag4.Series[0].Points.Clear();
            chart_lmag5.Series[0].Points.Clear();
            chart_lmag6.Series[0].Points.Clear();
            chart_lmag7.Series[0].Points.Clear();
            chart_lmag8.Series[0].Points.Clear();
            chart_lmag9.Series[0].Points.Clear();
            chart_lmag10.Series[0].Points.Clear();

            for (int i = 0; i < 20; i++)
            {
                Serialport1XyAutoSet[i] = CheckedData[i * 2 + pointIndex + 2] * 256 + CheckedData[i * 2 + 1 + pointIndex + 2];
            }
            UpdateSerialport1DataQueueValue(Serialport1XyAutoSet[0], Serialport1XyAutoSet[1], Serialport1XyAutoSet[2], Serialport1XyAutoSet[3]);
            UpdateSerialport2DataQueueValue(Serialport1XyAutoSet[4], Serialport1XyAutoSet[5], Serialport1XyAutoSet[6], Serialport1XyAutoSet[7]);
            UpdateSerialport3DataQueueValue(Serialport1XyAutoSet[8], Serialport1XyAutoSet[9], Serialport1XyAutoSet[10], Serialport1XyAutoSet[11]);
            UpdateSerialport4DataQueueValue(Serialport1XyAutoSet[12], Serialport1XyAutoSet[13], Serialport1XyAutoSet[14], Serialport1XyAutoSet[15]);
            UpdateSerialport5DataQueueValue(Serialport1XyAutoSet[16], Serialport1XyAutoSet[17], Serialport1XyAutoSet[18], Serialport1XyAutoSet[19]);



            for (int i = 0; i < 100; i++)
            {
                this.chart_real1.Series[0].Points.AddXY((i + 1), Freq1RealDataQueue.ElementAt(i));
                this.chart_lmag1.Series[0].Points.AddXY((i + 1), Freq1ImagDataQueue.ElementAt(i));
                this.chart_real2.Series[0].Points.AddXY((i + 1), Freq2RealDataQueue.ElementAt(i));
                this.chart_lmag2.Series[0].Points.AddXY((i + 1), Freq2ImagDataQueue.ElementAt(i));
                this.chart_real3.Series[0].Points.AddXY((i + 1), Freq3RealDataQueue.ElementAt(i));
                this.chart_lmag3.Series[0].Points.AddXY((i + 1), Freq3ImagDataQueue.ElementAt(i));
                this.chart_real4.Series[0].Points.AddXY((i + 1), Freq4RealDataQueue.ElementAt(i));
                this.chart_lmag4.Series[0].Points.AddXY((i + 1), Freq4ImagDataQueue.ElementAt(i));
                this.chart_real5.Series[0].Points.AddXY((i + 1), Freq5RealDataQueue.ElementAt(i));
                this.chart_lmag5.Series[0].Points.AddXY((i + 1), Freq5ImagDataQueue.ElementAt(i));
                this.chart_real6.Series[0].Points.AddXY((i + 1), Freq6RealDataQueue.ElementAt(i));
                this.chart_lmag6.Series[0].Points.AddXY((i + 1), Freq6ImagDataQueue.ElementAt(i));
                this.chart_real7.Series[0].Points.AddXY((i + 1), Freq7RealDataQueue.ElementAt(i));
                this.chart_lmag7.Series[0].Points.AddXY((i + 1), Freq7ImagDataQueue.ElementAt(i));
                this.chart_real8.Series[0].Points.AddXY((i + 1), Freq8RealDataQueue.ElementAt(i));
                this.chart_lmag8.Series[0].Points.AddXY((i + 1), Freq8ImagDataQueue.ElementAt(i));
                this.chart_real9.Series[0].Points.AddXY((i + 1), Freq9RealDataQueue.ElementAt(i));
                this.chart_lmag9.Series[0].Points.AddXY((i + 1), Freq9ImagDataQueue.ElementAt(i));
                this.chart_real10.Series[0].Points.AddXY((i + 1), Freq10RealDataQueue.ElementAt(i));
                this.chart_lmag10.Series[0].Points.AddXY((i + 1), Freq10ImagDataQueue.ElementAt(i));
            }//
            pointIndex += 40;
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked)
            {
                //自动发送功能选中,开始自动发送
                numericUpDown1.Enabled = false;
                timer2.Interval = (int)numericUpDown1.Value;
                timer2.Start();
            }
            else
            {
                //自动发送功能未选中,停止自动发送
                numericUpDown1.Enabled = true;
                timer2.Stop();
            }
        }

        /// <summary>
        /// 计算偏移值
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button7_Click(object sender, EventArgs e)
        {
           
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
            {
                /* 启动定时器 */
                numericUpDown2.Enabled = false;
                timer3.Interval = (int)numericUpDown2.Value;
                timer3.Start();
            }
            else
            {
                /* 取消时间戳，停止定时器 */
                numericUpDown2.Enabled = true;
                timer3.Stop();
            }

        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            /* 设置时间内未收到数据，分包，加入时间戳 */
            is_need_time = true;

        }



        private void btnStart_Click(object sender, EventArgs e)
        {
            if (th == null || !th.IsAlive)
            {
                th = new Thread(Run);
                th.IsBackground = true;
                th.Start();
            }
            timeStart = System.DateTime.Now;
            timer2.Enabled = true;
            timer3.Enabled = true;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (th != null && th.IsAlive)
            {
                th.Abort();
            }
            timer2.Enabled = false;
            timer3.Enabled = false;
        }


        public void Run()
        {

            /*while (SerialPortReceiveData.Count > 0)
            {
                Thread.Sleep(100);
                try
                {
                    this.BeginInvoke((EventHandler)(delegate
                    {

                        while (SerialPortReceiveData.Count > 40)
                        {
*//*                            this.chart_real1.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[0] * 256 + SerialPortReceiveData[1] );
                            this.chart_lmag1.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[2] * 256 + SerialPortReceiveData[3] );
                            this.chart_real1.ChartAreas[0].AxisX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);
                            this.chart_lmag1.ChartAreas[0].Ax   isX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);


                            this.chart_real2.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[4] * 256 + SerialPortReceiveData[5] );
                            this.chart_lmag2.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[6] * 256 + SerialPortReceiveData[7] );
                            this.chart_real2.ChartAreas[0].AxisX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);
                            this.chart_lmag2.ChartAreas[0].AxisX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);

                            this.chart_real3.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[8] * 256 + SerialPortReceiveData[9]);
                            this.chart_lmag3.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[10] * 256 + SerialPortReceiveData[11]);
                            this.chart_real3.ChartAreas[0].AxisX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);
                            this.chart_lmag3.ChartAreas[0].AxisX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);

                            this.chart_real4.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[12] * 256 + SerialPortReceiveData[13]);
                            this.chart_lmag4.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[14] * 256 + SerialPortReceiveData[15]);
                            this.chart_real4.ChartAreas[0].AxisX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);
                            this.chart_lmag4.ChartAreas[0].AxisX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);

                            this.chart_real5.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[16] * 256 + SerialPortReceiveData[17]);
                            this.chart_lmag5.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[18] * 256 + SerialPortReceiveData[19]);
                            this.chart_real5.ChartAreas[0].AxisX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);
                            this.chart_lmag5.ChartAreas[0].AxisX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);

                            this.chart_real6.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[20] * 256 + SerialPortReceiveData[21]);
                            this.chart_lmag6.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[22] * 256 + SerialPortReceiveData[23]);
                            this.chart_real6.ChartAreas[0].AxisX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);
                            this.chart_lmag6.ChartAreas[0].AxisX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);

                            this.chart_real7.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[24] * 256 + SerialPortReceiveData[25]);
                            this.chart_lmag7.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[26] * 256 + SerialPortReceiveData[27]);
                            this.chart_real7.ChartAreas[0].AxisX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);
                            this.chart_lmag7.ChartAreas[0].AxisX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);

                            this.chart_real8.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[28] * 256 + SerialPortReceiveData[29]);
                            this.chart_lmag8.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[30] * 256 + SerialPortReceiveData[31]);
                            this.chart_real8.ChartAreas[0].AxisX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);
                            this.chart_lmag8.ChartAreas[0].AxisX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);

                            this.chart_real9.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[32] * 256 + SerialPortReceiveData[33]);
                            this.chart_lmag9.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[34] * 256 + SerialPortReceiveData[35]);
                            this.chart_real9.ChartAreas[0].AxisX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);
                            this.chart_lmag9.ChartAreas[0].AxisX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);

                            this.chart_real10.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[36] * 256 + SerialPortReceiveData[37]);
                            this.chart_lmag10.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[38] * 256 + SerialPortReceiveData[39]);
                            this.chart_real10.ChartAreas[0].AxisX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);
                            this.chart_lmag10.ChartAreas[0].AxisX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);*//*

                            real1.Add(SerialPortReceiveData[0] * 256 + SerialPortReceiveData[1]);
                            real2.Add(SerialPortReceiveData[4] * 256 + SerialPortReceiveData[5]);
                            real3.Add(SerialPortReceiveData[8] * 256 + SerialPortReceiveData[9]);
                            real4.Add(SerialPortReceiveData[12] * 256 + SerialPortReceiveData[13]);
                            real5.Add(SerialPortReceiveData[16] * 256 + SerialPortReceiveData[17]);
                            real6.Add(SerialPortReceiveData[20] * 256 + SerialPortReceiveData[21]);
                            real7.Add(SerialPortReceiveData[24] * 256 + SerialPortReceiveData[25]);
                            real8.Add(SerialPortReceiveData[28] * 256 + SerialPortReceiveData[29]);
                            real9.Add(SerialPortReceiveData[32] * 256 + SerialPortReceiveData[33]);
                            real10.Add(SerialPortReceiveData[36] * 256 + SerialPortReceiveData[37]);

                            lmag1.Add(SerialPortReceiveData[2] * 256 + SerialPortReceiveData[3]);
                            lmag2.Add(SerialPortReceiveData[6] * 256 + SerialPortReceiveData[7]);
                            lmag3.Add(SerialPortReceiveData[10] * 256 + SerialPortReceiveData[11]);
                            lmag4.Add(SerialPortReceiveData[14] * 256 + SerialPortReceiveData[15]);
                            lmag5.Add(SerialPortReceiveData[18] * 256 + SerialPortReceiveData[19]);
                            lmag6.Add(SerialPortReceiveData[22] * 256 + SerialPortReceiveData[23]);
                            lmag7.Add(SerialPortReceiveData[26] * 256 + SerialPortReceiveData[27]);
                            lmag8.Add(SerialPortReceiveData[30] * 256 + SerialPortReceiveData[31]);
                            lmag9.Add(SerialPortReceiveData[34] * 256 + SerialPortReceiveData[35]);
                            lmag10.Add(SerialPortReceiveData[38] * 256 + SerialPortReceiveData[39]);

                            SerialPortReceiveData.RemoveRange(0, 40);
                        }
                        
                        pointIndex++;

                    }));


                }
                catch (Exception ex)
                {
                    string s = ex.Message;
                }
            }*/
        }


        private string GetDataStr(List<byte> list, int num)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < (list.Count() / 40); i++)
            {
                sb.Append((list[num*2 + i * 40] * 256 + list[num*2 + 1 + i * 40]).ToString() + ' ');
            }


            return sb.ToString();
        }

        private double GetMax(List<byte> Data, int num)
        {
            List<int> list = new List<int>();
            for (int i = 0; i < (Data.Count() / 40); i++)
            {
                list.Add(Data[num*2 + i * 40] * 256 + Data[num*2 + 1 + i * 40]);
            }
            list = list.Where(x => x != 0).ToList();
            double Max = 0;
            int count = list.Count();
            int w = 64;
            const int c1 = 3;
            int[] arr = new int[1 + c1 * 2];
            double[] data = new double[count];

            for (int j = 0; j < c1; j++)
            {
                // 中值滤波
                int c2 = 1 + j * 2;
                for (int k = 0; k < count; k++)
                {
                    int n = list[k];
                    if (j > 0 && k >= j && k < count - j)
                    {
                        for (int l = 0; l < c2; l++)
                        {
                            arr[l] = list[k - j + l];
                        }
                        Array.Sort(arr);
                        n = arr[j];
                    }
                    data[k] = n;

                }

            }

            // 滑动窗口内最大偏差(比较基准：直线方程，首尾点连线，之前已经做了中值滤波，无需再计算均值点)
            for (int k = w; k < count; k++)
            {
                double vMax = 0;
                {
                    Point pt1 = new Point(0, data[k - w]);
                    Point pt2 = new Point(w - 1, data[k - 1]);
                    // 计算斜率
                    double slope = (pt2.Y - pt1.Y) / (pt2.X - pt1.X);
                    // 计算y轴截距
                    double yIntercept = (pt1.Y - slope * pt1.X);
                    for (int l = 1; l < w - 1; l++)
                    {
                        Point pt = new Point(l, data[k - w + l]);
                        double dist = Math.Abs((slope * pt.X + yIntercept) - pt.Y) / Math.Sqrt(slope * slope + 1);
                        if (vMax < dist)
                        {
                            vMax = dist;
                        }
                    }

                }
                if ((Max < vMax) & (vMax < 100) )
                {
                    Max = vMax;
                }
            }
         
            return Max;
        }

        public void UpdateSerialport1DataQueueValue(int data1, int data2, int data3, int data4)
        {
            if (Freq1RealDataQueue.Count > 100)
            {
                //先出列
                for (int i = 0; i < 1; i++)
                {
                    Freq1RealDataQueue.Dequeue();//出队
                }
            }
            for (int i = 0; i < 1; i++)
            {
                Freq1RealDataQueue.Enqueue(data1);//进队
            }/////////
            if (Freq1ImagDataQueue.Count > 100)
            {
                //先出列
                for (int i = 0; i < 1; i++)
                {
                    Freq1ImagDataQueue.Dequeue();//出队
                }
            }
            for (int i = 0; i < 1; i++)
            {
                Freq1ImagDataQueue.Enqueue(data2);//进队
            }////////
            if (Freq2RealDataQueue.Count > 100)
            {
                //先出列
                for (int i = 0; i < 1; i++)
                {
                    Freq2RealDataQueue.Dequeue();//出队
                }
            }
            for (int i = 0; i < 1; i++)
            {
                Freq2RealDataQueue.Enqueue(data3);//进队
            }////////
            if (Freq2ImagDataQueue.Count > 100)
            {
                //先出列
                for (int i = 0; i < 1; i++)
                {
                    Freq2ImagDataQueue.Dequeue();//出队
                }
            }
            for (int i = 0; i < 1; i++)
            {
                Freq2ImagDataQueue.Enqueue(data4);//进队
            }
        }

        public void UpdateSerialport2DataQueueValue(int data1, int data2, int data3, int data4)
        {
            if (Freq3RealDataQueue.Count > 100)
            {
                //先出列
                for (int i = 0; i < 1; i++)
                {
                    Freq3RealDataQueue.Dequeue();//出队
                }
            }
            for (int i = 0; i < 1; i++)
            {
                Freq3RealDataQueue.Enqueue(data1);//进队
            }/////////
            if (Freq3ImagDataQueue.Count > 100)
            {
                //先出列
                for (int i = 0; i < 1; i++)
                {
                    Freq3ImagDataQueue.Dequeue();//出队
                }
            }
            for (int i = 0; i < 1; i++)
            {
                Freq3ImagDataQueue.Enqueue(data2);//进队
            }////////
            if (Freq4RealDataQueue.Count > 100)
            {
                //先出列
                for (int i = 0; i < 1; i++)
                {
                    Freq4RealDataQueue.Dequeue();//出队
                }
            }
            for (int i = 0; i < 1; i++)
            {
                Freq4RealDataQueue.Enqueue(data3);//进队
            }////////
            if (Freq4ImagDataQueue.Count > 100)
            {
                //先出列
                for (int i = 0; i < 1; i++)
                {
                    Freq4ImagDataQueue.Dequeue();//出队
                }
            }
            for (int i = 0; i < 1; i++)
            {
                Freq4ImagDataQueue.Enqueue(data4);//进队
            }
        }

        public void UpdateSerialport3DataQueueValue(int data1, int data2, int data3, int data4)
        {
            if (Freq5RealDataQueue.Count > 100)
            {
                //先出列
                for (int i = 0; i < 1; i++)
                {
                    Freq5RealDataQueue.Dequeue();//出队
                }
            }
            for (int i = 0; i < 1; i++)
            {
                Freq5RealDataQueue.Enqueue(data1);//进队
            }/////////
            if (Freq5ImagDataQueue.Count > 100)
            {
                //先出列
                for (int i = 0; i < 1; i++)
                {
                    Freq5ImagDataQueue.Dequeue();//出队
                }
            }
            for (int i = 0; i < 1; i++)
            {
                Freq5ImagDataQueue.Enqueue(data2);//进队
            }////////
            if (Freq6RealDataQueue.Count > 100)
            {
                //先出列
                for (int i = 0; i < 1; i++)
                {
                    Freq6RealDataQueue.Dequeue();//出队
                }
            }
            for (int i = 0; i < 1; i++)
            {
                Freq6RealDataQueue.Enqueue(data3);//进队
            }////////
            if (Freq6ImagDataQueue.Count > 100)
            {
                //先出列
                for (int i = 0; i < 1; i++)
                {
                    Freq6ImagDataQueue.Dequeue();//出队
                }
            }
            for (int i = 0; i < 1; i++)
            {
                Freq6ImagDataQueue.Enqueue(data4);//进队
            }
        }

        public void UpdateSerialport4DataQueueValue(int data1, int data2, int data3, int data4)
        {
            if (Freq7RealDataQueue.Count > 100)
            {
                //先出列
                for (int i = 0; i < 1; i++)
                {
                    Freq7RealDataQueue.Dequeue();//出队
                }
            }
            for (int i = 0; i < 1; i++)
            {
                Freq7RealDataQueue.Enqueue(data1);//进队
            }/////////
            if (Freq7ImagDataQueue.Count > 100)
            {
                //先出列
                for (int i = 0; i < 1; i++)
                {
                    Freq7ImagDataQueue.Dequeue();//出队
                }
            }
            for (int i = 0; i < 1; i++)
            {
                Freq7ImagDataQueue.Enqueue(data2);//进队
            }////////
            if (Freq8RealDataQueue.Count > 100)
            {
                //先出列
                for (int i = 0; i < 1; i++)
                {
                    Freq8RealDataQueue.Dequeue();//出队
                }
            }
            for (int i = 0; i < 1; i++)
            {
                Freq8RealDataQueue.Enqueue(data3);//进队
            }////////
            if (Freq8ImagDataQueue.Count > 100)
            {
                //先出列
                for (int i = 0; i < 1; i++)
                {
                    Freq8ImagDataQueue.Dequeue();//出队
                }
            }
            for (int i = 0; i < 1; i++)
            {
                Freq8ImagDataQueue.Enqueue(data4);//进队
            }
        }

        public void UpdateSerialport5DataQueueValue(int data1, int data2, int data3, int data4)
        {
            if (Freq9RealDataQueue.Count > 100)
            {
                //先出列
                for (int i = 0; i < 1; i++)
                {
                    Freq9RealDataQueue.Dequeue();//出队
                }
            }
            for (int i = 0; i < 1; i++)
            {
                Freq9RealDataQueue.Enqueue(data1);//进队
            }/////////
            if (Freq9ImagDataQueue.Count > 100)
            {
                //先出列
                for (int i = 0; i < 1; i++)
                {
                    Freq9ImagDataQueue.Dequeue();//出队
                }
            }
            for (int i = 0; i < 1; i++)
            {
                Freq9ImagDataQueue.Enqueue(data2);//进队
            }////////
            if (Freq10RealDataQueue.Count > 100)
            {
                //先出列
                for (int i = 0; i < 1; i++)
                {
                    Freq10RealDataQueue.Dequeue();//出队
                }
            }
            for (int i = 0; i < 1; i++)
            {
                Freq10RealDataQueue.Enqueue(data3);//进队
            }////////
            if (Freq10ImagDataQueue.Count > 100)
            {
                //先出列
                for (int i = 0; i < 1; i++)
                {
                    Freq10ImagDataQueue.Dequeue();//出队
                }
            }
            for (int i = 0; i < 1; i++)
            {
                Freq10ImagDataQueue.Enqueue(data4);//进队
            }
        }

        private void timer4_Tick(object sender, EventArgs e)
        {
            chart_real1.ChartAreas[0].AxisY.Minimum = Serialport1XyAutoSet[0] - 100;
            chart_real1.ChartAreas[0].AxisY.Maximum = Serialport1XyAutoSet[0] + 100;
            chart_lmag1.ChartAreas[0].AxisY.Minimum = Serialport1XyAutoSet[1] - 100;
            chart_lmag1.ChartAreas[0].AxisY.Maximum = Serialport1XyAutoSet[1] + 100;
            chart_real2.ChartAreas[0].AxisY.Minimum = Serialport1XyAutoSet[2] - 100;
            chart_real2.ChartAreas[0].AxisY.Maximum = Serialport1XyAutoSet[2] + 100;
            chart_lmag2.ChartAreas[0].AxisY.Minimum = Serialport1XyAutoSet[3] - 100;
            chart_lmag2.ChartAreas[0].AxisY.Maximum = Serialport1XyAutoSet[3] + 100;
            chart_real3.ChartAreas[0].AxisY.Minimum = Serialport1XyAutoSet[4] - 100;
            chart_real3.ChartAreas[0].AxisY.Maximum = Serialport1XyAutoSet[4] + 100;
            chart_lmag3.ChartAreas[0].AxisY.Minimum = Serialport1XyAutoSet[5] - 100;
            chart_lmag3.ChartAreas[0].AxisY.Maximum = Serialport1XyAutoSet[5] + 100;
            chart_real4.ChartAreas[0].AxisY.Minimum = Serialport1XyAutoSet[6] - 100;
            chart_real4.ChartAreas[0].AxisY.Maximum = Serialport1XyAutoSet[6] + 100;
            chart_lmag4.ChartAreas[0].AxisY.Minimum = Serialport1XyAutoSet[7] - 100;
            chart_lmag4.ChartAreas[0].AxisY.Maximum = Serialport1XyAutoSet[7] + 100;
            chart_real5.ChartAreas[0].AxisY.Minimum = Serialport1XyAutoSet[8] - 100;
            chart_real5.ChartAreas[0].AxisY.Maximum = Serialport1XyAutoSet[8] + 100;
            chart_lmag5.ChartAreas[0].AxisY.Minimum = Serialport1XyAutoSet[9] - 100;
            chart_lmag5.ChartAreas[0].AxisY.Maximum = Serialport1XyAutoSet[9] + 100;
            chart_real6.ChartAreas[0].AxisY.Minimum = Serialport1XyAutoSet[10] - 100;
            chart_real6.ChartAreas[0].AxisY.Maximum = Serialport1XyAutoSet[10] + 100;
            chart_lmag6.ChartAreas[0].AxisY.Minimum = Serialport1XyAutoSet[11] - 100;
            chart_lmag6.ChartAreas[0].AxisY.Maximum = Serialport1XyAutoSet[11] + 100;
            chart_real7.ChartAreas[0].AxisY.Minimum = Serialport1XyAutoSet[12] - 100;
            chart_real7.ChartAreas[0].AxisY.Maximum = Serialport1XyAutoSet[12] + 100;
            chart_lmag7.ChartAreas[0].AxisY.Minimum = Serialport1XyAutoSet[13] - 100;
            chart_lmag7.ChartAreas[0].AxisY.Maximum = Serialport1XyAutoSet[13] + 100;
            chart_real8.ChartAreas[0].AxisY.Minimum = Serialport1XyAutoSet[14] - 100;
            chart_real8.ChartAreas[0].AxisY.Maximum = Serialport1XyAutoSet[14] + 100;
            chart_lmag8.ChartAreas[0].AxisY.Minimum = Serialport1XyAutoSet[15] - 100;
            chart_lmag8.ChartAreas[0].AxisY.Maximum = Serialport1XyAutoSet[15] + 100;
            chart_real9.ChartAreas[0].AxisY.Minimum = Serialport1XyAutoSet[16] - 100;
            chart_real9.ChartAreas[0].AxisY.Maximum = Serialport1XyAutoSet[16] + 100;
            chart_lmag9.ChartAreas[0].AxisY.Minimum = Serialport1XyAutoSet[17] - 100;
            chart_lmag9.ChartAreas[0].AxisY.Maximum = Serialport1XyAutoSet[17] + 100;
            chart_real10.ChartAreas[0].AxisY.Minimum = Serialport1XyAutoSet[18] - 100;
            chart_real10.ChartAreas[0].AxisY.Maximum = Serialport1XyAutoSet[18] + 100;
            chart_lmag10.ChartAreas[0].AxisY.Minimum = Serialport1XyAutoSet[19] - 100;
            chart_lmag10.ChartAreas[0].AxisY.Maximum = Serialport1XyAutoSet[19] + 100;
        }
    }
}

