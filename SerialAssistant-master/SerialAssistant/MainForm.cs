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
using System.Runtime.InteropServices;//控制台
using MathWorks.MATLAB.NET.Arrays;//MWArray
using emdfNative;

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

        //使用控制台
        [DllImport("kernel32.dll")]
        public static extern bool AllocConsole();

        private List<int> real1 = new List<int>();
        private List<int> real2 = new List<int>();
        private List<int> real3 = new List<int>();
        private List<int> real4 = new List<int>();
        private List<int> real5 = new List<int>();
        private List<int> real6 = new List<int>();
        private List<int> real7 = new List<int>();
        private List<int> real8 = new List<int>();
        private List<int> real9 = new List<int>();
        private List<int> real10 = new List<int>();
                     
        private List<int> lmag1 = new List<int>();
        private List<int> lmag2 = new List<int>();
        private List<int> lmag3 = new List<int>();
        private List<int> lmag4 = new List<int>();
        private List<int> lmag5 = new List<int>();
        private List<int> lmag6 = new List<int>();
        private List<int> lmag7 = new List<int>();
        private List<int> lmag8 = new List<int>();
        private List<int> lmag9 = new List<int>();
        private List<int> lmag10 = new List<int>();


        public MainForm()
        {
            InitializeComponent();
            AllocConsole(); //关联一个控制台窗口用于显示信息
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

            String fileNamereal1;
            String fileNamereal2;
            String fileNamereal3;
            String fileNamereal4;
            String fileNamereal5;
            String fileNamereal6;
            String fileNamereal7;
            String fileNamereal8;
            String fileNamereal9;
            String fileNamereal10;

            String fileNamelmag1;
            String fileNamelmag2;
            String fileNamelmag3;
            String fileNamelmag4;
            String fileNamelmag5;
            String fileNamelmag6;
            String fileNamelmag7;
            String fileNamelmag8;
            String fileNamelmag9;
            String fileNamelmag10;

            string foldPath;

            /* 获取当前接收区内容 */
            String recv_data = textBox1.Text;

            /*String real1_str = "通道1实部：" + GetDataStr(real1);
            String real2_str = "通道2实部：" + GetDataStr(real2);
            String real3_str = "通道3实部：" + GetDataStr(real3);
            String real4_str = "通道4实部：" + GetDataStr(real4);
            String real5_str = "通道5实部：" + GetDataStr(real5);
            String real6_str = "通道6实部：" + GetDataStr(real6);
            String real7_str = "通道7实部：" + GetDataStr(real7);
            String real8_str = "通道8实部：" + GetDataStr(real8);
            String real9_str = "通道9实部：" + GetDataStr(real9);
            String real10_str = "通道10实部：" + GetDataStr(real10);
            
            String lmag1_str =  "通道1虚部：" + GetDataStr(lmag1);
            String lmag2_str =  "通道2虚部：" + GetDataStr(lmag2);
            String lmag3_str =  "通道3虚部：" + GetDataStr(lmag3);
            String lmag4_str =  "通道4虚部：" + GetDataStr(lmag4);
            String lmag5_str =  "通道5虚部：" + GetDataStr(lmag5);
            String lmag6_str =  "通道6虚部：" + GetDataStr(lmag6);
            String lmag7_str =  "通道7虚部：" + GetDataStr(lmag7);
            String lmag8_str =  "通道8虚部：" + GetDataStr(lmag8);
            String lmag9_str =  "通道9虚部：" + GetDataStr(lmag9);
            String lmag10_str = "通道10虚部：" + GetDataStr(lmag10);*/

            String real1_str = GetDataStr(real1);
            String real2_str = GetDataStr(real2);
            String real3_str = GetDataStr(real3);
            String real4_str = GetDataStr(real4);
            String real5_str = GetDataStr(real5);
            String real6_str = GetDataStr(real6);
            String real7_str = GetDataStr(real7);
            String real8_str = GetDataStr(real8);
            String real9_str = GetDataStr(real9);
            String real10_str =GetDataStr(real10);

            String lmag1_str = GetDataStr(lmag1);
            String lmag2_str = GetDataStr(lmag2);
            String lmag3_str = GetDataStr(lmag3);
            String lmag4_str = GetDataStr(lmag4);
            String lmag5_str = GetDataStr(lmag5);
            String lmag6_str = GetDataStr(lmag6);
            String lmag7_str = GetDataStr(lmag7);
            String lmag8_str = GetDataStr(lmag8);
            String lmag9_str = GetDataStr(lmag9);
            String lmag10_str = GetDataStr(lmag10);

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

            //fileName = foldPath + "\\" + "log" + "_" + time.ToString("yyyy_MM_dd_HH_mm_ss") + ".txt";
            fileNamereal1 = foldPath + "\\" + "log" + "_real1_" + ".txt";
            fileNamereal2 = foldPath + "\\" + "log" + "_real2_" + ".txt";
            fileNamereal3 = foldPath + "\\" + "log" + "_real3_" + ".txt";
            fileNamereal4 = foldPath + "\\" + "log" + "_real4_" + ".txt";
            fileNamereal5 = foldPath + "\\" + "log" + "_real5_" + ".txt";
            fileNamereal6 = foldPath + "\\" + "log" + "_real6_" + ".txt";
            fileNamereal7 = foldPath + "\\" + "log" + "_real7_" + ".txt";
            fileNamereal8 = foldPath + "\\" + "log" + "_real8_" + ".txt";
            fileNamereal9 = foldPath + "\\" + "log" + "_real9_" + ".txt";
            fileNamereal10 = foldPath + "\\" + "log" + "_real10_" + ".txt";

            fileNamelmag1 = foldPath + "\\" + "log" + "_lmag1_" + ".txt";
            fileNamelmag2 = foldPath + "\\" + "log" + "_lmag2_" + ".txt";
            fileNamelmag3 = foldPath + "\\" + "log" + "_lmag3_" + ".txt";
            fileNamelmag4 = foldPath + "\\" + "log" + "_lmag4_" + ".txt";
            fileNamelmag5 = foldPath + "\\" + "log" + "_lmag5_" + ".txt";
            fileNamelmag6 = foldPath + "\\" + "log" + "_lmag6_" + ".txt";
            fileNamelmag7 = foldPath + "\\" + "log" + "_lmag7_" + ".txt";
            fileNamelmag8 = foldPath + "\\" + "log" + "_lmag8_" + ".txt";
            fileNamelmag9 = foldPath + "\\" + "log" + "_lmag9_" + ".txt";
            fileNamelmag10 = foldPath + "\\" + "log" + "_lmag10_" + ".txt";



            try
            {
                /* 保存串口接收区的内容 */
                //创建 FileStream 类的实例
                FileStream fileStreamreal1 = new FileStream(fileNamereal1, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileStream fileStreamreal2 = new FileStream(fileNamereal2, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileStream fileStreamreal3 = new FileStream(fileNamereal3, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileStream fileStreamreal4 = new FileStream(fileNamereal4, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileStream fileStreamreal5 = new FileStream(fileNamereal5, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileStream fileStreamreal6 = new FileStream(fileNamereal6, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileStream fileStreamreal7 = new FileStream(fileNamereal7, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileStream fileStreamreal8 = new FileStream(fileNamereal8, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileStream fileStreamreal9 = new FileStream(fileNamereal9, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileStream fileStreamreal10 = new FileStream(fileNamereal10, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

                FileStream fileStreamlmag1 = new FileStream(fileNamelmag1, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileStream fileStreamlmag2 = new FileStream(fileNamelmag2, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileStream fileStreamlmag3 = new FileStream(fileNamelmag3, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileStream fileStreamlmag4 = new FileStream(fileNamelmag4, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileStream fileStreamlmag5 = new FileStream(fileNamelmag5, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileStream fileStreamlmag6 = new FileStream(fileNamelmag6, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileStream fileStreamlmag7 = new FileStream(fileNamelmag7, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileStream fileStreamlmag8 = new FileStream(fileNamelmag8, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileStream fileStreamlmag9 = new FileStream(fileNamelmag9, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                FileStream fileStreamlmag10 = new FileStream(fileNamelmag10, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

                //将字符串转换为字节数组
                //byte[] bytes = Encoding.UTF8.GetBytes(recv_data);
                byte[] bytes_real1 = Encoding.UTF8.GetBytes(real1_str);
                byte[] bytes_real2 = Encoding.UTF8.GetBytes(real2_str);
                byte[] bytes_real3 = Encoding.UTF8.GetBytes(real3_str);
                byte[] bytes_real4 = Encoding.UTF8.GetBytes(real4_str);
                byte[] bytes_real5 = Encoding.UTF8.GetBytes(real5_str);
                byte[] bytes_real6 = Encoding.UTF8.GetBytes(real6_str);
                byte[] bytes_real7 = Encoding.UTF8.GetBytes(real7_str);
                byte[] bytes_real8 = Encoding.UTF8.GetBytes(real8_str);
                byte[] bytes_real9 = Encoding.UTF8.GetBytes(real9_str);
                byte[] bytes_real10 = Encoding.UTF8.GetBytes(real10_str);

                byte[] bytes_lmag1 = Encoding.UTF8.GetBytes(lmag1_str);
                byte[] bytes_lmag2 = Encoding.UTF8.GetBytes(lmag2_str);
                byte[] bytes_lmag3 = Encoding.UTF8.GetBytes(lmag3_str);
                byte[] bytes_lmag4 = Encoding.UTF8.GetBytes(lmag4_str);
                byte[] bytes_lmag5 = Encoding.UTF8.GetBytes(lmag5_str);
                byte[] bytes_lmag6 = Encoding.UTF8.GetBytes(lmag6_str);
                byte[] bytes_lmag7 = Encoding.UTF8.GetBytes(lmag7_str);
                byte[] bytes_lmag8 = Encoding.UTF8.GetBytes(lmag8_str);
                byte[] bytes_lmag9 = Encoding.UTF8.GetBytes(lmag9_str);
                byte[] bytes_lmag10 = Encoding.UTF8.GetBytes(lmag10_str);


                //向文件中写入字节数组
                fileStreamreal1.Write(bytes_real1, 0, bytes_real1.Length);
                fileStreamlmag1.Write(bytes_lmag1, 0, bytes_lmag1.Length);

                fileStreamreal2.Write(bytes_real2, 0, bytes_real2.Length);
                fileStreamlmag2.Write(bytes_lmag2, 0, bytes_lmag2.Length);

                fileStreamreal3.Write(bytes_real3, 0, bytes_real3.Length);
                fileStreamlmag3.Write(bytes_lmag3, 0, bytes_lmag3.Length);

                fileStreamreal4.Write(bytes_real4, 0, bytes_real4.Length);
                fileStreamlmag4.Write(bytes_lmag4, 0, bytes_lmag4.Length);

                fileStreamreal5.Write(bytes_real5, 0, bytes_real5.Length);
                fileStreamlmag5.Write(bytes_lmag5, 0, bytes_lmag5.Length);

                fileStreamreal6.Write(bytes_real6, 0, bytes_real6.Length);
                fileStreamlmag6.Write(bytes_lmag6, 0, bytes_lmag6.Length);

                fileStreamreal7.Write(bytes_real7, 0, bytes_real7.Length);
                fileStreamlmag7.Write(bytes_lmag7, 0, bytes_lmag7.Length);

                fileStreamreal8.Write(bytes_real8, 0, bytes_real8.Length);
                fileStreamlmag8.Write(bytes_lmag8, 0, bytes_lmag8.Length);

                fileStreamreal9.Write(bytes_real9, 0, bytes_real9.Length);
                fileStreamlmag9.Write(bytes_lmag9, 0, bytes_lmag9.Length);

                fileStreamreal10.Write(bytes_real10, 0, bytes_real10.Length);
                fileStreamlmag10.Write(bytes_lmag10, 0, bytes_lmag10.Length);

                //刷新缓冲区
                fileStreamreal1.Flush();
                fileStreamreal2.Flush();
                fileStreamreal3.Flush();
                fileStreamreal4.Flush();
                fileStreamreal5.Flush();
                fileStreamreal6.Flush();
                fileStreamreal7.Flush();
                fileStreamreal8.Flush();
                fileStreamreal9.Flush();
                fileStreamreal10.Flush();

                fileStreamlmag1.Flush();
                fileStreamlmag2.Flush();
                fileStreamlmag3.Flush();
                fileStreamlmag4.Flush();
                fileStreamlmag5.Flush();
                fileStreamlmag6.Flush();
                fileStreamlmag7.Flush();
                fileStreamlmag8.Flush();
                fileStreamlmag9.Flush();
                fileStreamlmag10.Flush();

                //关闭流
                //fileStream.Close();
                fileStreamreal1.Close();
                fileStreamreal2.Close();
                fileStreamreal3.Close();
                fileStreamreal4.Close();
                fileStreamreal5.Close();
                fileStreamreal6.Close();
                fileStreamreal7.Close();
                fileStreamreal8.Close();
                fileStreamreal9.Close();
                fileStreamreal10.Close();

                fileStreamlmag1.Close();
                fileStreamlmag2.Close();
                fileStreamlmag3.Close();
                fileStreamlmag4.Close();
                fileStreamlmag5.Close();
                fileStreamlmag6.Close();
                fileStreamlmag7.Close();
                fileStreamlmag8.Close();
                fileStreamlmag9.Close();
                fileStreamlmag10.Close();

                //提示用户
                MessageBox.Show("日志已保存!(" + fileNamereal1 + ")");
            }
            catch (Exception ex)
            {
                //提示用户
                MessageBox.Show("发生异常!(" + ex.ToString() + ")");
            }

        }
        #endregion


        #region 测试MATLAB动态库按钮
        private void button3_Click(object sender, EventArgs e)
        {
            /*string file;

            *//* 弹出文件选择框供用户选择 *//*
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

            *//* 读取文件内容 *//*
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
            }*/

            ToMatlab(real1);

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
                            this.chart_real1.ChartAreas[0].AxisX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);
                            this.chart_lmag1.ChartAreas[0].AxisX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);


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
                            this.chart_lmag10.ChartAreas[0].AxisX.ScaleView.Scroll(System.Windows.Forms.DataVisualization.Charting.ScrollType.Last);

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
                            lmag2.Add(SerialPortReceiveData[6] * 256 + SerialPortReceiveData[4]);
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
            }
        }


        private string GetDataStr(List<int> list)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in list)
            {
                sb.Append(item.ToString() + ' ');
            }
            //int listDev = maxListDev(list);
            //sb.Append("该通道的最大差值为：" + listDev.ToString() + '\n');

            return sb.ToString();
        }

        private void ToMatlab(List<int> list)
        {
            if (list.Count() != 0)
            {
                //声明数组并把list类型转化成都变了类型
                int[] temp = list.ToArray();
                //声明二维数组，并把数组类型转化成二维数组
                int[,] temp2 = new int[temp.Length, 1];

                for (int i = 0; i < temp.Length; i++)
                {
                    temp2[i, 0] = temp[i];
                }

                //声明MWArray，并把二维数组转换成MWArray类型
                MWArray temp3 = new MWNumericArray(temp2);

                Class1 testemdf = new Class1();
                object test;
                object test2;
                test = testemdf.emdf(temp3,2);
                test2 = testemdf.emdf(temp3,3);



                System.Console.WriteLine(test);
            }
           

        }

    }
}

