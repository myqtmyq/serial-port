using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 串口通信
{
    public delegate void ShowWindow();
    public delegate void HideWindow();
    public delegate void OpenPort();
    public delegate void ClosePort();
    public delegate Point GetMainPos();
    public delegate int GetMainWidth();
    public partial class MainForm : Form
    {
        Drawer Displayer;    //实例化Drawer窗口，使得在 MainForm  可操作Drawer

        //***********************************创建&显示绘图窗口 | 初始化类成员委托**********************************//
        private void CreateNewDrawer()//创建Drawer窗口
        {
            Displayer = new Drawer();//创建新对象
            Displayer.ShowMainWindow = new ShowWindow(ShowMe);//初始化类成员委托
            Displayer.HideMainWindow = new HideWindow(HideMe);
            Displayer.GetMainPos = new GetMainPos(GetMyPos);
            Displayer.CloseSerialPort = new ClosePort(ClosePort);
            Displayer.OpenSerialPort = new OpenPort(OpenPort);
            Displayer.GetMainWidth = new GetMainWidth(GetMyWidth);
            Displayer.Show();//显示窗口
        }
        //************************************* MainForm 窗口初始化************************************************//
        public MainForm()
        {
            InitializeComponent();
            serialPort1.Encoding = Encoding.GetEncoding("GB2312");                    //串口接收编码--实现汉字显示
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;     //不检查线程
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            for (int i = 1; i < 20; i++)
            {
                comboBox1.Items.Add("COM" + i.ToString());                                        //添加串口
            }
            comboBox1.Text = "COM2";                                                              //默认选项
            comboBox2.Text = "4800";
            comboBox1.SelectedIndex = 0;
        }

        //************************************* 委托 函数 编写************************************************//

        public void ClosePort()//关闭串口，供委托调用
        {
            try
            {
                serialPort1.Close();
            }
            catch (System.Exception)
            {

            }
        }

        private Point GetMyPos()//供委托调用
        {
            return this.Location;
        }

        public void OpenPort()//打开串口，供委托调用
        {
            try
            {
                serialPort1.Open();
            }
            catch (System.Exception)
            {
                MessageBox.Show("串口打开失败，请检查", "错误");
            }
        }

        public void ShowMe()//供委托调用
        {
            this.Show();
        }

        public void HideMe()//供委托调用
        {
            this.Hide();
        }

        int GetMyWidth()//供委托调用
        {
            return this.Width;
        }

        //*************************************串口接受数据 处理函数************************************************//
        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            if (!radioButton3.Checked)
            {
                textBox1.AppendText(serialPort1.ReadExisting());                                //串口类会自动处理汉字，所以不需要特别转换
            }
            else
            {
                try
                {
                    byte[] data = new byte[serialPort1.BytesToRead];                                //定义缓冲区，因为串口事件触发时有可能收到不止一个字节
                    serialPort1.Read(data, 0, data.Length);
                    if (Displayer != null)
                        Displayer.AddData(data);    //添加到绘图窗口到数据链表 
                    foreach (byte Member in data)                                                   //遍历用法
                    {
                        string str = Convert.ToString(Member, 16).ToUpper();
                        textBox1.AppendText("0x" + (str.Length == 1 ? "0" + str : str) + " ");
                    }
                }
                catch { }
            }
        }
        //**************创建绘图界面 & 设置界面大小 与 主窗口 在 显示器屏幕 平行最大化显示******************//
        private void CreateDisplayer()
        {
            this.Left = 0;
            CreateNewDrawer();
            Rectangle Rect = Screen.GetWorkingArea(this);
            Displayer.SetWindow(Rect.Width - this.Width, new Point(this.Width, this.Top));//设置绘制窗口宽度，以及坐标
        }

        //*************************************波形显示按钮************************************************//

        private void button4_Click(object sender, EventArgs e)
        {
            if (Displayer == null)//第一次创建Displayer = null
            {
                CreateDisplayer();
            }
            else
            {
                if (Displayer.IsDisposed)//多次创建通过判断IsDisposed确定串口是否已关闭，避免多次创建
                {
                    CreateDisplayer();
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                serialPort1.PortName = comboBox1.Text;                                              //端口号
                serialPort1.BaudRate = Convert.ToInt32(comboBox2.Text);                             //波特率
                serialPort1.Open();                                                                 //打开串口
                button1.Enabled = false;
                button2.Enabled = true;
            }
            catch
            {
                MessageBox.Show("端口错误", "错误");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            byte[] Data = new byte[1];                                                         //单字节发数据     
            if (serialPort1.IsOpen)
            {
                if (textBox2.Text != "")
                {
                    if (!radioButton1.Checked)
                    {
                        try
                        {
                            serialPort1.Write(textBox2.Text);
                            //serialPort1.WriteLine();                             //字符串写入
                        }
                        catch
                        {
                            MessageBox.Show("串口数据写入错误", "错误");
                        }
                    }
                    else                                                                    //数据模式
                    {
                        try                                                                 //如果此时用户输入字符串中含有非法字符（字母，汉字，符号等等，try，catch块可以捕捉并提示）
                        {
                            for (int i = 0; i < (textBox2.Text.Length - textBox2.Text.Length % 2) / 2; i++)//转换偶数个
                            {
                                Data[0] = Convert.ToByte(textBox2.Text.Substring(i * 2, 2), 16);           //转换
                                serialPort1.Write(Data, 0, 1);
                            }
                            if (textBox2.Text.Length % 2 != 0)
                            {
                                Data[0] = Convert.ToByte(textBox2.Text.Substring(textBox2.Text.Length - 1, 1), 16);//单独处理最后一个字符
                                serialPort1.Write(Data, 0, 1);                              //写入
                            }
                            //Data = Convert.ToByte(textBox2.Text.Substring(textBox2.Text.Length - 1, 1), 16);
                            //  }
                        }
                        catch
                        {
                            MessageBox.Show("数据转换错误，请输入数字。", "错误");
                        }
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                serialPort1.Close();                                                            //关闭串口        
                button1.Enabled = true;
                button2.Enabled = false;
            }
            catch
            {

            }
        }

        private void button3_Click_1(object sender, EventArgs e)
        {

        }
    }
}
