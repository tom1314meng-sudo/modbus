using Modbus.Device;
using System;
using System.Linq;
using System.Net.Sockets;

namespace modbus
{
    /// <summary>
    /// Modbus TCP 通讯封装
    /// </summary>
    public class TCPHelper : IDisposable
    {
        private TcpClient _tcpClient;
        private ModbusIpMaster _master;

        public bool IsConnected => _master != null;

        public void Connect(string ip, int port)
        {
            if (_master != null)
            {
                throw new InvalidOperationException("TCP已连接");
            }
            _tcpClient = new TcpClient(ip, port);
            _master = ModbusIpMaster.CreateIp(_tcpClient);
        }

        public void Disconnect()
        {
            _master?.Dispose();
            _master = null;
            if (_tcpClient != null)
            {
                _tcpClient.Close();
                _tcpClient = null;
            }
        }

        private void EnsureConnected()
        {
            if (_master == null)
            {
                throw new InvalidOperationException("请先建立TCP连接");
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

        public static bool ParseBool(string text)
        {
            string val = text.Trim();
            return val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}