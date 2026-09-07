using Sunny.UI;
using System;
using System.IO.Ports;
using System.Windows.Forms;

namespace modbus
{
    public partial class Form1 : Form
    {
        private readonly RTUHelper _rtu = new RTUHelper();
        private readonly TCPHelper _tcp = new TCPHelper();

        public string[] BaudRate_Array = new string[] { "300", "600", "1200", "2400", "4800", "9600", "14400", "19200", "38400", "56000" };
        public string[] DataBits_Array = new string[] { "7", "8" };
        public string[] CheckBits_Array = new string[] { "None", "Even", "ODD" };
        public string[] StopBits_Array = new string[] { "1", "2" };  //停止位

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] portNames = SerialPort.GetPortNames();
            uiComboBox1.Items.AddRange(portNames);
            uiComboBox2.Items.AddRange(BaudRate_Array);
            uiComboBox3.Items.AddRange(DataBits_Array);
            uiComboBox4.Items.AddRange(CheckBits_Array);
            uiComboBox5.Items.AddRange(StopBits_Array);
        }

        // ============ RTU 串口 ============

        private void uiButton1_Click(object sender, EventArgs e)
        {
            try
            {
                if (_rtu.IsOpen)
                {
                    _rtu.Close();
                    UIMessageTip.Show("串口已关闭");
                }
                else
                {
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

        // ============ TCP ============

        private void uiButton6_Click(object sender, EventArgs e)
        {
            try
            {
                _tcp.Connect(uiTextBox3.Text, Convert.ToInt32(uiTextBox4.Text));
                UIMessageTip.ShowOk("TCP连接成功");
            }
            catch (Exception ex)
            {
                UIMessageTip.ShowError(ex.Message, 3000);
            }
        }

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
    }
}