using Modbus.Device;
using System;
using System.IO.Ports;
using System.Linq;

namespace modbus
{
    /// <summary>
    /// Modbus RTU（串口）通讯封装
    /// </summary>
    public class RTUHelper : IDisposable
    {
        private SerialPort _serialPort = new SerialPort();
        private ModbusSerialMaster _master;

        public bool IsOpen => _serialPort.IsOpen;

        public void Open(string portName, int baudRate, int dataBits, Parity parity, StopBits stopBits)
        {
            if (_serialPort.IsOpen)
            {
                throw new InvalidOperationException("串口已打开");
            }
            _serialPort.PortName = portName;
            _serialPort.BaudRate = baudRate;
            _serialPort.DataBits = dataBits;
            _serialPort.Parity = parity;
            _serialPort.StopBits = stopBits;
            _serialPort.Open();
            _master = ModbusSerialMaster.CreateRtu(_serialPort);
        }

        public void Close()
        {
            _master?.Dispose();
            _master = null;
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        public static Parity ParseParity(string s)
        {
            switch (s)
            {
                case "None": return Parity.None;
                case "Even": return Parity.Even;
                case "ODD": return Parity.Odd;
                default: return Parity.None;
            }
        }

        private void EnsureConnected()
        {
            if (_master == null)
            {
                throw new InvalidOperationException("请先打开串口");
            }
        }

        public ushort[] ReadHoldingRegisters(byte slaveId, ushort startAddress, ushort numberOfPoints)
        {
            EnsureConnected();
            return _master.ReadHoldingRegisters(slaveId, startAddress, numberOfPoints);
        }

        public bool[] ReadCoils(byte slaveId, ushort startAddress, ushort numberOfPoints)
        {
            EnsureConnected();
            return _master.ReadCoils(slaveId, startAddress, numberOfPoints);
        }

        public void WriteSingleRegister(byte slaveId, ushort address, ushort value)
        {
            EnsureConnected();
            _master.WriteSingleRegister(slaveId, address, value);
        }

        public void WriteMultipleRegisters(byte slaveId, ushort startAddress, ushort[] values)
        {
            EnsureConnected();
            _master.WriteMultipleRegisters(slaveId, startAddress, values);
        }

        public void WriteSingleCoil(byte slaveId, ushort address, bool value)
        {
            EnsureConnected();
            _master.WriteSingleCoil(slaveId, address, value);
        }

        public void WriteMultipleCoils(byte slaveId, ushort startAddress, bool[] values)
        {
            EnsureConnected();
            _master.WriteMultipleCoils(slaveId, startAddress, values);
        }

        public static bool[] ParseBoolArray(string text)
        {
            return text.Split(',')
                       .Select(s => s.Trim() == "1" || s.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
                       .ToArray();
        }

        public static ushort[] ParseUShortArray(string text)
        {
            return text.Split(',')
                       .Select(s => ushort.Parse(s.Trim()))
                       .ToArray();
        }

        public void Dispose()
        {
            Close();
            _serialPort?.Dispose();
            _serialPort = null;
        }
    }
}