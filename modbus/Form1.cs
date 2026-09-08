using Sunny.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace modbus
{
    /// <summary>
    /// 主窗体。所有 Modbus 通讯逻辑均委托给 <see cref="RTUHelper"/> 与 <see cref="TCPHelper"/>，
    /// 本类只负责：装载界面元素、读取界面输入、调用 helper、显示提示。
    /// </summary>
    public partial class Form1 : Form
    {
        // 串口（RTU）通讯封装
        private readonly RTUHelper _rtu = new RTUHelper();

        // TCP 通讯封装
        private readonly TCPHelper _tcp = new TCPHelper();

        // INI 读写封装
        private readonly iniHelper _iniHelper = new iniHelper();

        // 串口参数下拉框数据源
        public string[] BaudRate_Array = new string[] { "300", "600", "1200", "2400", "4800", "9600", "14400", "19200", "38400", "56000" };
        public string[] DataBits_Array = new string[] { "7", "8" };
        public string[] CheckBits_Array = new string[] { "None", "Even", "ODD" };
        public string[] StopBits_Array = new string[] { "1", "2" };  //停止位

        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 窗体加载：填充串口列表与各参数下拉框。
        /// </summary>
        private void Form1_Load(object sender, EventArgs e)
        {
            // 自动枚举系统串口，写入下拉框
            string[] portNames = SerialPort.GetPortNames();
            uiComboBox1.Items.AddRange(portNames);
            uiComboBox2.Items.AddRange(BaudRate_Array);
            uiComboBox3.Items.AddRange(DataBits_Array);
            uiComboBox4.Items.AddRange(CheckBits_Array);
            uiComboBox5.Items.AddRange(StopBits_Array);
        }

        // ============================================================
        // RTU 区（uiGroupBox1）
        // ============================================================

        /// <summary>
        /// 串口连接/断开 切换按钮（uiButton1）。
        /// </summary>
        private void uiButton1_Click(object sender, EventArgs e)
        {
            try
            {
                if (_rtu.IsOpen)
                {
                    // 当前已打开，执行关闭
                    _rtu.Close();
                    UIMessageTip.Show("串口已关闭");
                }
                else
                {
                    // 从界面读取串口参数并打开（超时 1000ms、重试 3 次，使用默认值）
                    _rtu.Open(
                        uiComboBox1.SelectedItem.ToString(),
                        int.Parse(uiComboBox2.SelectedItem.ToString()),
                        int.Parse(uiComboBox3.SelectedItem.ToString()),
                        RTUHelper.ParseParity(uiComboBox4.SelectedItem.ToString()),
                        (StopBits)Enum.Parse(typeof(StopBits), uiComboBox5.SelectedItem.ToString()));
                    UIMessageTip.ShowOk("串口已打开");
                }
            }
            catch (Exception ex)
            {
                UIMessageTip.ShowError(ex.Message, 3000);
            }
        }

        /// <summary>
        /// RTU 读保持寄存器（uiButton2），从站号 1，起始 0，长度 10。
        /// </summary>
        private void uiButton2_Click(object sender, EventArgs e)
        {
            try
            {
                uiListBox1.Items.Clear();
                ushort[] result = _rtu.ReadHoldingRegisters(1, 0, 10);
                for (int i = 0; i < result.Length; i++)
                {
                    uiListBox1.Items.Add(result[i]);
                }
                UIMessageTip.ShowOk("读取成功");
            }
            catch (Exception ex)
            {
                UIMessageTip.ShowError(ex.Message, 3000);
            }
        }

        /// <summary>
        /// RTU 读线圈（uiButton12），从站号 1，起始 0，长度 10。
        /// </summary>
        private void uiButton12_Click(object sender, EventArgs e)
        {
            try
            {
                bool[] bools = _rtu.ReadCoils(1, 0, 10);
                for (int i = 0; i < bools.Length; i++)
                {
                    uiListBox1.Items.Add(bools[i]);
                }
                UIMessageTip.ShowOk("读取成功");
            }
            catch (Exception ex)
            {
                UIMessageTip.ShowError(ex.Message, 3000);
            }
        }

        /// <summary>
        /// RTU 写单寄存器（uiButton3）：
        /// 地址取自 uiTextBox1，值取自 uiTextBox2，从站号固定 1。
        /// </summary>
        private void uiButton3_Click(object sender, EventArgs e)
        {
            try
            {
                ushort address = Convert.ToUInt16(uiTextBox1.Text);
                ushort value = Convert.ToUInt16(uiTextBox2.Text);
                _rtu.WriteSingleRegister(1, address, value);
                UIMessageTip.ShowOk("写入成功");
            }
            catch (Exception ex)
            {
                UIMessageTip.ShowError(ex.Message, 3000);
            }
        }

        // ============================================================
        // TCP 区（uiGroupBox2）
        // ============================================================

        /// <summary>
        /// TCP 连接按钮（uiButton6）：IP 取自 uiTextBox3，端口取自 uiTextBox4。
        /// </summary>
        private void uiButton6_Click(object sender, EventArgs e)
        {
            try
            {
                //关闭旧连接，先释放资源
                 _tcp.Disconnect();
                _tcp.Connect(uiTextBox3.Text, Convert.ToInt32(uiTextBox4.Text));
                UIMessageTip.ShowOk("TCP连接成功");
            }
            catch (Exception ex)
            {
                UIMessageTip.ShowError(ex.Message, 3000);
            }
        }

        /// <summary>
        /// TCP 读保持寄存器（uiButton5）。
        /// </summary>
        private void uiButton5_Click(object sender, EventArgs e)
        {
            try
            {
                uiListBox1.Items.Clear();
                ushort[] result = _tcp.ReadHoldingRegisters(1, 0, 10);
                for (int i = 0; i < result.Length; i++)
                {
                    uiListBox1.Items.Add(result[i]);
                }
                UIMessageTip.ShowOk("读取成功");
            }
            catch (Exception ex)
            {
                UIMessageTip.ShowError(ex.Message, 3000);
            }
        }

        /// <summary>
        /// TCP 写单寄存器（uiButton4）。
        /// </summary>
        private void uiButton4_Click(object sender, EventArgs e)
        {
            try
            {
                ushort address = Convert.ToUInt16(uiTextBox1.Text);
                ushort value = Convert.ToUInt16(uiTextBox2.Text);
                _tcp.WriteSingleRegister(1, address, value);
                UIMessageTip.ShowOk("TCP写入成功");
            }
            catch (Exception ex)
            {
                UIMessageTip.ShowError(ex.Message, 3000);
            }
        }

        /// <summary>
        /// TCP 写多寄存器（uiButton13）：
        /// 起始地址取 uiTextBox1，多个值取自 uiTextBox8（英文逗号分隔）。
        /// </summary>
        private void uiButton13_Click(object sender, EventArgs e)
        {
            try
            {
                ushort startAddress = Convert.ToUInt16(uiTextBox1.Text);
                ushort[] values = TCPHelper.ParseUShortArray(uiTextBox8.Text);
                _tcp.WriteMultipleRegisters(1, startAddress, values);
                UIMessageTip.ShowOk("TCP写入多个成功");
            }
            catch (Exception ex)
            {
                UIMessageTip.ShowError(ex.Message, 3000);
            }
        }

        // ============================================================
        // 共用：写线圈（自动选择 RTU 或 TCP 通道）
        // ============================================================

        /// <summary>
        /// 写单线圈（uiButton7）：优先使用已打开的串口，否则回退到 TCP。
        /// </summary>
        private void uiButton7_Click(object sender, EventArgs e)
        {
            try
            {
                ushort address = Convert.ToUInt16(uiTextBox1.Text);
                bool value = TCPHelper.ParseBool(uiTextBox2.Text);

                if (_rtu.IsOpen)
                {
                    _rtu.WriteSingleCoil(1, address, value);
                }
                else if (_tcp.IsConnected)
                {
                    _tcp.WriteSingleCoil(1, address, value);
                }
                else
                {
                    throw new Exception("请先打开串口或建立TCP连接");
                }
                UIMessageTip.ShowOk("写入单线圈成功");
            }
            catch (Exception ex)
            {
                UIMessageTip.ShowError(ex.Message, 3000);
            }
        }

        /// <summary>
        /// 写多线圈（uiButton8）：优先使用已打开的串口，否则回退到 TCP。
        /// </summary>
        private void uiButton8_Click(object sender, EventArgs e)
        {
            try
            {
                ushort startAddress = Convert.ToUInt16(uiTextBox1.Text);
                bool[] values = TCPHelper.ParseBoolArray(uiTextBox8.Text);

                if (_rtu.IsOpen)
                {
                    _rtu.WriteMultipleCoils(1, startAddress, values);
                }
                else if (_tcp.IsConnected)
                {
                    _tcp.WriteMultipleCoils(1, startAddress, values);
                }
                else
                {
                    throw new Exception("请先打开串口或建立TCP连接");
                }
                UIMessageTip.ShowOk("写入多线圈成功");
            }
            catch (Exception ex)
            {
                UIMessageTip.ShowError(ex.Message, 3000);
            }
        }

        // ============================================================
        // TCP 额外按钮（与 uiButton4/13/16/9 重复的 TCP 写）
        // ============================================================

        /// <summary>
        /// TCP 写单寄存器（uiButton18），与 uiButton4 行为一致。
        /// </summary>
        private void uiButton18_Click(object sender, EventArgs e)
        {
            try
            {
                ushort address = Convert.ToUInt16(uiTextBox1.Text);
                ushort value = Convert.ToUInt16(uiTextBox2.Text);
                _tcp.WriteSingleRegister(1, address, value);
                UIMessageTip.ShowOk("TCP写入成功");
            }
            catch (Exception ex)
            {
                UIMessageTip.ShowError(ex.Message, 3000);
            }
        }

        /// <summary>
        /// TCP 写多寄存器（uiButton17），与 uiButton13 行为一致。
        /// </summary>
        private void uiButton17_Click(object sender, EventArgs e)
        {
            try
            {
                ushort startAddress = Convert.ToUInt16(uiTextBox1.Text);
                ushort[] values = TCPHelper.ParseUShortArray(uiTextBox8.Text);
                _tcp.WriteMultipleRegisters(1, startAddress, values);
                UIMessageTip.ShowOk("TCP写入多个成功");
            }
            catch (Exception ex)
            {
                UIMessageTip.ShowError(ex.Message, 3000);
            }
        }

        /// <summary>
        /// TCP 写单线圈（uiButton16）。
        /// </summary>
        private void uiButton16_Click(object sender, EventArgs e)
        {
            try
            {
                ushort address = Convert.ToUInt16(uiTextBox1.Text);
                bool value = TCPHelper.ParseBool(uiTextBox2.Text);
                _tcp.WriteSingleCoil(1, address, value);
                UIMessageTip.ShowOk("TCP写入单线圈成功");
            }
            catch (Exception ex)
            {
                UIMessageTip.ShowError(ex.Message, 3000);
            }
        }

        /// <summary>
        /// TCP 写多线圈（uiButton9）。
        /// </summary>
        private void uiButton9_Click(object sender, EventArgs e)
        {
            try
            {
                ushort startAddress = Convert.ToUInt16(uiTextBox1.Text);
                bool[] values = TCPHelper.ParseBoolArray(uiTextBox8.Text);
                _tcp.WriteMultipleCoils(1, startAddress, values);
                UIMessageTip.ShowOk("TCP写入多线圈成功");
            }
            catch (Exception ex)
            {
                UIMessageTip.ShowError(ex.Message, 3000);
            }
        }

        private void uiButton10_Click(object sender, EventArgs e)
        {
            try
            {
                List<string> list = new List<string>();
                list.Add(uiTextBox6.Text);
                list.Add(uiTextBox5.Text);
                list.Add(uiTextBox7.Text);
                List<ushort> ushortlist = list.Select(ushort.Parse).ToList();//转换ushort类型
                _tcp.WriteMultipleRegisters(1, 0, ushortlist.ToArray());
                _iniHelper.WriteLocal(uiTextBox6, uiTextBox5, uiTextBox7);
                UIMessageTip.ShowOk("写入成功");
            }
            catch (Exception ex)
            {

                UIMessageTip.ShowError(ex.Message, 3000);
            }

        }

        
        // plc读取
        private void uiButton11_Click(object sender, EventArgs e)
        {
            try
            {
                uiListBox1.Items.Clear();
                ushort[] ushorts = new ushort[10];
               ushorts = _tcp.ReadHoldingRegisters(1, 0, 10);
                for (int i = 0; i < ushorts.Length; i++)
                {
                    uiListBox1.Items.Add(ushorts[i]);
                }
                UIMessageTip.ShowOk("读取成功");
            }
            catch (Exception ex)
            {

                UIMessageTip.ShowError(ex.Message, 3000);
            }
        }
        // 读取本地配置
        private void uiButton9_Click_1(object sender, EventArgs e)
        {
            _iniHelper.LoadLocal(uiTextBox6, uiTextBox5, uiTextBox7);
            UIMessageTip.ShowOk("读取成功");
        }

        bool ismonitoring = false;  

        private void uiButton8_Click_1(object sender, EventArgs e)
        {
            if (!ismonitoring)
            {
                UIMessageTip.ShowWarning("未监控");
                ismonitoring = true;
                Task.Run(() => monitorloop());
            }
            else
            {
                UIMessageTip.ShowWarning("正在监控中");
                
            }
        }
        private void monitorloop()
        {
            UILedBulb[] uILedBulbs = new UILedBulb[] { uiLedBulb1, uiLedBulb2, uiLedBulb3, uiLedBulb4, uiLedBulb5, uiLedBulb6, uiLedBulb7, uiLedBulb8 };
            while (ismonitoring)
            {
                try
                {
                    ushort[] ushorts = _tcp.ReadHoldingRegisters(1, 0, 8);
                    Invoke(new Action(() =>
                    {
                        if (ushorts != null)
                        {
                            for (int i = 0; i < ushorts.Length; i++)
                            {
                                if (ushorts[i] > 10) { 
                                    uILedBulbs[i].Color = Color.Red;

                                }
                                else
                                {
                                    uILedBulbs[i].Color = Color.Green;
                                }

                            }
                        }
                    }));
                }
                catch (Exception ex)
                {
                    UIMessageTip.ShowError(ex.Message, 3000);
                    ismonitoring = false;
                }

            }
        }

        private void uiButton7_Click_1(object sender, EventArgs e) 
        {
            ismonitoring = false;
            
            UIMessageTip.ShowOk("监控已停止");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            ismonitoring = false;
            _rtu.Close();
            _tcp.Disconnect();

        }
    }
}