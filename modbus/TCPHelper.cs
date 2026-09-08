using Modbus.Device;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace modbus
{
    /// <summary>
    /// Modbus TCP 通讯封装。
    /// 对外屏蔽 <see cref="TcpClient"/> 与 <see cref="ModbusIpMaster"/> 的细节，
    /// 仅暴露连接管理、寄存器/线圈读写与文本解析等方法。
    /// </summary>
    public class TCPHelper : IDisposable
    {
        // 底层 TCP 客户端，整个 TCPHelper 生命周期内复用
        private TcpClient _tcpClient;

        // Modbus TCP 主站，仅在连接成功后非空
        private ModbusIpMaster _master;

        /// <summary>
        /// 当前是否已建立 TCP 连接。
        /// </summary>
        public bool IsConnected => _master != null;

        /// <summary>
        /// 连接到指定 IP 与端口，并创建 TCP 主站。
        /// 连接会按 <paramref name="retryCount"/> 次数进行重试，
        /// 每次连接的超时时间为 <paramref name="timeoutMs"/> 毫秒。
        /// </summary>
        /// <param name="ip">服务器 IP 地址</param>
        /// <param name="port">服务器端口（Modbus TCP 默认为 502）</param>
        /// <param name="timeoutMs">单次连接超时时间（毫秒）</param>
        /// <param name="retryCount">失败后重试次数（不含首次）</param>
        public void Connect(string ip, int port, int timeoutMs = 1000, int retryCount = 3)
        {
            if (_master != null)
            {
                throw new InvalidOperationException("TCP已连接");
            }
            if (timeoutMs < 1) timeoutMs = 1000;
            if (retryCount < 0) retryCount = 0;

            Exception lastEx = null;
            int attempts = retryCount + 1;
            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    // 使用支持超时的构造函数，超时后抛出 SocketException
                    _tcpClient = new TcpClient { SendTimeout = timeoutMs, ReceiveTimeout = timeoutMs };
                    var task = _tcpClient.ConnectAsync(ip, port);
                    if (!task.Wait(timeoutMs))
                    {
                        throw new TimeoutException($"连接 {ip}:{port} 超时（{timeoutMs}ms）");
                    }
                    if (!_tcpClient.Connected)
                    {
                        throw new SocketException();
                    }
                    // 基于已连接的 TcpClient 创建 Modbus TCP 主站
                    _master = ModbusIpMaster.CreateIp(_tcpClient);
                    if (_master != null)
                    {
                        _master.Transport.ReadTimeout = timeoutMs;
                        _master.Transport.WriteTimeout = timeoutMs;
                    }
                    return;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    // 释放半连接的 TcpClient，准备下次重试
                    try { _tcpClient?.Close(); } catch { }
                    _tcpClient = null;
                    if (i < attempts - 1)
                    {
                        Thread.Sleep(timeoutMs);
                    }
                }
            }
            throw new InvalidOperationException(
                $"TCP连接失败（已重试 {retryCount} 次，超时 {timeoutMs}ms）：{lastEx?.Message}", lastEx);
        }

        /// <summary>
        /// 断开连接并释放资源。可重复调用。
        /// </summary>
        public void Disconnect()
        {
            _master?.Dispose();
            _master = null;
            if (_tcpClient != null)
            {
                _tcpClient.Close();
                _tcpClient.Dispose();
                _tcpClient = null;
            }
        }

        /// <summary>
        /// 确保 TCP 主站已创建，否则抛出明确错误提示调用方先连接。
        /// </summary>
        private void EnsureConnected()
        {
            if (_master == null)
            {
                throw new InvalidOperationException("请先建立TCP连接");
            }
        }

        /// <summary>
        /// 读保持寄存器（功能码 0x03）。
        /// </summary>
        public ushort[] ReadHoldingRegisters(byte slaveId, ushort startAddress, ushort numberOfPoints)
        {
            EnsureConnected();
            return _master.ReadHoldingRegisters(slaveId, startAddress, numberOfPoints);
        }

        /// <summary>
        /// 读线圈（功能码 0x01）。
        /// </summary>
        public bool[] ReadCoils(byte slaveId, ushort startAddress, ushort numberOfPoints)
        {
            EnsureConnected();
            return _master.ReadCoils(slaveId, startAddress, numberOfPoints);
        }

        /// <summary>
        /// 写单个保持寄存器（功能码 0x06）。
        /// </summary>
        public void WriteSingleRegister(byte slaveId, ushort address, ushort value)
        {
            EnsureConnected();
            _master.WriteSingleRegister(slaveId, address, value);
        }

        /// <summary>
        /// 写多个保持寄存器（功能码 0x10）。
        /// </summary>
        public void WriteMultipleRegisters(byte slaveId, ushort startAddress, ushort[] values)
        {
            EnsureConnected();
            _master.WriteMultipleRegisters(slaveId, startAddress, values);
        }

        /// <summary>
        /// 写单个线圈（功能码 0x05）。
        /// </summary>
        public void WriteSingleCoil(byte slaveId, ushort address, bool value)
        {
            EnsureConnected();
            _master.WriteSingleCoil(slaveId, address, value);
        }

        /// <summary>
        /// 写多个线圈（功能码 0x0F）。
        /// </summary>
        public void WriteMultipleCoils(byte slaveId, ushort startAddress, bool[] values)
        {
            EnsureConnected();
            _master.WriteMultipleCoils(slaveId, startAddress, values);
        }

        /// <summary>
        /// 把界面输入的 "1,0,1" / "true,false" 文本解析为 bool 数组。
        /// </summary>
        public static bool[] ParseBoolArray(string text)
        {
            return text.Split(',')
                       .Select(s => s.Trim() == "1" || s.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
                       .ToArray();
        }

        /// <summary>
        /// 把界面输入的 "10,20,30" 文本解析为 ushort 数组。
        /// </summary>
        public static ushort[] ParseUShortArray(string text)
        {
            return text.Split(',')
                       .Select(s => ushort.Parse(s.Trim()))
                       .ToArray();
        }

        /// <summary>
        /// 把界面输入的单值文本（"1"/"0"/"true"/"false"）解析为 bool。
        /// </summary>
        public static bool ParseBool(string text)
        {
            string val = text.Trim();
            return val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 实现 <see cref="IDisposable"/>，释放 TCP 资源。
        /// </summary>
        public void Dispose()
        {
            Disconnect();
        }
    }
}