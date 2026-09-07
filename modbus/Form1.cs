using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using Sunny.UI;
using Modbus.Device;

namespace modbus
{
    public partial class Form1 : Form
    {
        SerialPort serialPort = new SerialPort();
        ModbusSerialMaster modbusSerivalMaster = null;  
        public string[] BaudRate_Array = new string[] { "300", "600", "1200", "2400", "4800", "9600", "14400", "19200", "38400", "56000" };
        public string[] DataBits_Array = new string[] {  "7", "8" };
        public string[] CheckBits_Array = new string[] { "None","Even","ODD" };
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

        private void uiButton1_Click(object sender, EventArgs e)
        {
            try
            {
                if (serialPort.IsOpen)
                {
                    closeSerialPort();
                }
                else
                {
                    OpenSerialPort();
                }
            }
            catch (Exception ex)
            {

               UIMessageTip.ShowError(ex.Message, 3000);
            }
        }

        private void OpenSerialPort()
        {
            serialPort.PortName = uiComboBox1.SelectedItem.ToString();
            serialPort.BaudRate = int.Parse(uiComboBox2.SelectedItem.ToString());
            serialPort.DataBits = int.Parse(uiComboBox3.SelectedItem.ToString());
            serialPort.Parity = CheckBits(uiComboBox4.SelectedItem.ToString());
            serialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), uiComboBox5.SelectedItem.ToString());
            serialPort.Open();
            modbusSerivalMaster = ModbusSerialMaster.CreateRtu(serialPort);
            UIMessageTip.ShowOk("串口已打开");
        }

        private void closeSerialPort()
        {
            //先释放modbus资源
            modbusSerivalMaster?.Dispose();
            modbusSerivalMaster = null;
            serialPort.Close();
            UIMessageTip.Show("串口已关闭");
        }

        private Parity CheckBits(string s)
        {
            switch (s)
            {
                case "None":
                    return Parity.None;
                case "Even":
                    return Parity.Even;
                case "ODD":
                    return Parity.Odd;
                default:
                   return Parity.None;
            }
        }
    }
}
