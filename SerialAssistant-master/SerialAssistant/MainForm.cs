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
        private long send_count = 0;    //发送字节计数
        private StringBuilder sb = new StringBuilder();    //为了避免在接收处理函数中反复调用，依然声明为一个全局变量
        private DateTime current_time = new DateTime();    //为了避免在接收处理函数中反复调用，依然声明为一个全局变量
        private bool is_need_time = true;
        private List<byte> buffer = new List<byte>(4096); //设置缓存处理CRC32串口的校验
        private int ReceiveDataNum = 40;   //数据位  40 个字节
        private int ReceiveCheckIndex = 42; //检验位一个字节，帧头一字节，len一字节，数据位加校验位41个字节，所以校验位是第43位，即数组index=42
        public static bool intimewindowIsOpen = false; //判断波形窗口是否创建
        private List<byte> SerialPortReceiveData = new List<byte>(); //用于存储串口的数据
        Thread th;
        private int pointIndex = 0;//x轴的点




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

            int index = 1;
            while (buffer.Count > 0x2C) //最短协议长度
            {
                if (buffer[0] == 0xAA) //协议头
                {
                    if (buffer[index] != 0x80) //查询协议尾
                    {
                        index++;

                        if (index > buffer.Count) //没有接收到0x80协议尾
                        {
                            break; //退出继续接收 
                        }
                    }
                    else //接收到协议尾  得到完整一帧数据
                    {
                        byte[] ReceiveBytes = new byte[ReceiveDataNum];//数据位
                        buffer.CopyTo(2, ReceiveBytes, 0, ReceiveDataNum);                                              

                        var randomCrc = CRC8(ReceiveBytes);//上位机计算的校验位
                        if (randomCrc == buffer[ReceiveCheckIndex]) //和传入的校验位进行校验
                        {
                            foreach (byte item in ReceiveBytes)
                            {
                                
                                SerialPortReceiveData.Add(item);
                            }
                            ShowSerialPortReceive(ReceiveBytes);
                        }
                        
                        buffer.RemoveRange(0, index);
                    }
                }
                else
                {
                    buffer.RemoveAt(0);
                }
            }

            #endregion

        }
        #endregion



        #region 显示串口接收的数据
        private void ShowSerialPortReceive(byte[] showbuffer)
        {
            sb.Clear();     //防止出错,首先清空字符串构造器 //不使用这个，就会重复显示输入的数据
            if (radioButton2.Checked)
            {
                //选中HEX模式显示
                foreach (byte b in showbuffer)
                {
                    sb.Append(b.ToString("X2") + ' ');    //将byte型数据转化为2位16进制文本显示，并用空格隔开                                    
                }
            }
            else
            {
                //选中ASCII模式显示
                sb.Append(Encoding.ASCII.GetString(showbuffer));  //将整个数组解码为ASCII数组
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
        public static byte CRC8(byte[] buffer)
        {
            byte crc = 0;
            for (int j = 0; j < buffer.Length; j++)
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
            return crc;
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
        }
        #endregion



        #region 下载按钮
        private void button4_Click(object sender, EventArgs e)
        {
            DateTime time = new DateTime();
            String fileName;
            string foldPath;

            /* 获取当前接收区内容 */
            String recv_data = textBox1.Text;
            if (recv_data.Equals(""))
            {
                MessageBox.Show("接收数据为空，无需保存！");
                return;
            }

            /* 获取当前时间，用于填充文件名 */
            //eg.log_2021_05_08_10_13_31.txt
            time = System.DateTime.Now;

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

            fileName = foldPath + "\\" + "log" + "_" + time.ToString("yyyy_MM_dd_HH_mm_ss") + ".txt";

            try
            {
                /* 保存串口接收区的内容 */
                //创建 FileStream 类的实例
                FileStream fileStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

                //将字符串转换为字节数组
                byte[] bytes = Encoding.UTF8.GetBytes(recv_data);

                //向文件中写入字节数组
                fileStream.Write(bytes, 0, bytes.Length);

                //刷新缓冲区
                fileStream.Flush();

                //关闭流
                fileStream.Close();

                //提示用户
                MessageBox.Show("日志已保存!(" + fileName + ")");
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
            string file;

            /* 弹出文件选择框供用户选择 */
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;//该值确定是否可以选择多个文件
            dialog.Title = "请选择要加载的文件(文本格式)";
            dialog.Filter = "文本文件(*.txt)|*.txt|JSON文件(*.json)|*.json";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                file = dialog.FileName;
            }
            else
            {
                return;
            }

            /* 读取文件内容 */
            try
            {
                //清空发送缓冲区
                //textBox2.Text = "";

                // 使用 StreamReader 来读取文件
                using (StreamReader sr = new StreamReader(file))
                {
                    string line;

                    // 从文件读取并显示行，直到文件的末尾 
                    while ((line = sr.ReadLine()) != null)
                    {
                        line = line + "\r\n";
                        //textBox2.AppendText(line);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载文件发生异常！(" + ex.ToString() + ")");
            }
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
            /*this.linkLabel1.Links[this.linkLabel1.Links.IndexOf(e.Link)].Visited = true;
            string targetUrl = "https://github.com/Mculover666/SerialAssistant";


            try
            {
                //尝试用edge打开
                System.Diagnostics.Process.Start("msedge.exe", targetUrl);
                return;
            }
            catch (Exception)
            {
                //edge它不香吗
            }

            try
            {
                //好吧，那用chrome
                System.Diagnostics.Process.Start("chrome.exe", targetUrl);
                return;
            }
            catch
            {
                //chrome不好用吗
            }
            try
            {
                //IE也不是不可以
                System.Diagnostics.Process.Start("iexplore.exe", targetUrl);
            }

            catch
            {
                //没救了，砸了吧！
            }*/
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
           // button5_Click(button5, new EventArgs());    //调用发送按钮回调函数
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

        private void button7_Click(object sender, EventArgs e)
        {
            /*string targetUrl = "http://www.mculover666.cn";

            try
            {
                //尝试用edge打开
                System.Diagnostics.Process.Start("msedge.exe", targetUrl);
                return;
            }
            catch (Exception)
            {
                //edge它不香吗
            }

            try
            {
                //好吧，那用chrome
                System.Diagnostics.Process.Start("chrome.exe", targetUrl);
                return;
            }
            catch
            {
                //chrome不好用吗
            }
            try
            {
                //IE也不是不可以
                System.Diagnostics.Process.Start("iexplore.exe", targetUrl);
                return;
            }

            catch
            {
                //没救了，砸了吧！
            }

            *//* 没办法了，提示一下 *//*
            MessageBox.Show("本软件开源免费，作者:Mculover666!");*/
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
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (th != null && th.IsAlive)
            {
                th.Abort();
            }
        }


        public void Run()
        {

            while (SerialPortReceiveData.Count > 0)
            {
                Thread.Sleep(100);
                try
                {
                    this.BeginInvoke((EventHandler)(delegate
                    {

                        while (SerialPortReceiveData.Count > 40)
                        {
                            this.chart_real1.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[0] * 256 + SerialPortReceiveData[1] );
                            this.chart_lmag1.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[2] * 256 + SerialPortReceiveData[3] );
                                                    
                            this.chart_real2.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[4] * 256 + SerialPortReceiveData[5] );
                            this.chart_lmag2.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[6] * 256 + SerialPortReceiveData[7] );
                                                    
                            this.chart_real3.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[8] * 256 + SerialPortReceiveData[9]);
                            this.chart_lmag3.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[10] * 256 + SerialPortReceiveData[11]);
                                                    
                            this.chart_real4.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[12] * 256 + SerialPortReceiveData[13]);
                            this.chart_lmag4.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[14] * 256 + SerialPortReceiveData[15]);
                                                    
                            this.chart_real5.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[16] * 256 + SerialPortReceiveData[17]);
                            this.chart_lmag5.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[18] * 256 + SerialPortReceiveData[19]);
                                                    
                            this.chart_real6.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[20] * 256 + SerialPortReceiveData[21]);
                            this.chart_lmag6.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[22] * 256 + SerialPortReceiveData[23]);
                                                    
                            this.chart_real7.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[24] * 256 + SerialPortReceiveData[25]);
                            this.chart_lmag7.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[26] * 256 + SerialPortReceiveData[27]);
                                                    
                            this.chart_real8.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[28] * 256 + SerialPortReceiveData[29]);
                            this.chart_lmag8.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[30] * 256 + SerialPortReceiveData[31]);
                                                    
                            this.chart_real9.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[32] * 256 + SerialPortReceiveData[33]);
                            this.chart_lmag9.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[34] * 256 + SerialPortReceiveData[35]);
                                                
                            this.chart_real10.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[36] * 256 + SerialPortReceiveData[37]);
                            this.chart_lmag10.Series[0].Points.AddXY(pointIndex, SerialPortReceiveData[38] * 256 + SerialPortReceiveData[39]);

                            SerialPortReceiveData.RemoveRange(0, 40);
                        }
                        
                        pointIndex++;

                    }));


                }
                catch (Exception ex)
                {
                    string s = ex.Message;
                }
            }
        }



  

    }
}

