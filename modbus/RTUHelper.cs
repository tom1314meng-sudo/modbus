using Modbus.Device;
using System;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace modbus
{
    /// <summary>
    /// Modbus RTU（串口）通讯封装。
    /// 对外屏蔽底层 <see cref="SerialPort"/> 与 <see cref="ModbusSerialMaster"/> 的细节，
    /// 仅暴露连接管理、寄存器/线圈读写与文本解析等方法。
    /// </summary>
    public class RTUHelper : IDisposable
    {
        // 底层串口对象，整个 RTUHelper 生命周期内复用
        private SerialPort _serialPort = new SerialPort();

        // Modbus 主站对象，仅在串口打开后非空
        private ModbusSerialMaster _master;

        /// <summary>
        /// 当前串口是否处于打开状态。
        /// </summary>
        public bool IsOpen => _serialPort.IsOpen;

        /// <summary>
        /// 按指定参数打开串口，并创建 RTU 主站。
        /// 打开过程会按 <paramref name="retryCount"/> 次数进行重试，
        /// 每次打开的最长等待时间为 <paramref name="timeoutMs"/> 毫秒。
        /// </summary>
        /// <param name="portName">串口名（如 COM1）</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="dataBits">数据位（7 或 8）</param>
        /// <param name="parity">校验位</param>
        /// <param name="stopBits">停止位</param>
        /// <param name="timeoutMs">单次打开超时时间（毫秒）</param>
        /// <param name="retryCount">失败后重试次数（不含首次）</param>
        public void Open(string portName, int baudRate, int dataBits, Parity parity, StopBits stopBits, int timeoutMs = 1000, int retryCount = 3)
        {
            if (_serialPort.IsOpen)
            {
                // 重复打开会导致 SerialPort 抛异常，这里提前拦截给出友好提示
                throw new InvalidOperationException("串口已打开");
            }
            if (timeoutMs < 1) timeoutMs = 1000;
            if (retryCount < 0) retryCount = 0;

            _serialPort.PortName = portName;
            _serialPort.BaudRate = baudRate;
            _serialPort.DataBits = dataBits;
            _serialPort.Parity = parity;
            _serialPort.StopBits = stopBits;
            // 串口层面的读写超时，配合后续 Modbus 超时一起生效
            _serialPort.ReadTimeout = timeoutMs;
            _serialPort.WriteTimeout = timeoutMs;

            Exception lastEx = null;
            int attempts = retryCount + 1;
            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    _serialPort.Open();
                    // 基于已打开的串口创建 RTU 主站，后续所有读写都通过 _master
                    _master = ModbusSerialMaster.CreateRtu(_serialPort);
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
                    // 关闭可能处于半开状态的串口，准备下次重试
                    try { if (_serialPort.IsOpen) _serialPort.Close(); } catch { }
                    if (i < attempts - 1)
                    {
                        Thread.Sleep(timeoutMs);
                    }
                }
            }
            throw new InvalidOperationException(
                $"串口打开失败（已重试 {retryCount} 次，超时 {timeoutMs}ms）：{lastEx?.Message}", lastEx);
        }

        /// <summary>
        /// 关闭串口并释放 Modbus 主站资源。
        /// 可重复调用，重复调用时不会抛错。
        /// </summary>
        public void Close()
        {
            // 先释放主站，否则底层串口可能还被占用
            _master?.Dispose();
            _master = null;
            if (_serialPort.IsOpen)
            {
                _serialPort.Dispose();
                _serialPort.Close();
            }
        }

        /// <summary>
        /// 把界面下拉框里的中文校验位文本转换成 <see cref="Parity"/> 枚举。
        /// </summary>
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

        /// <summary>
        /// 确保 RTU 主站已创建，否则抛出明确错误提示调用方先打开串口。
        /// </summary>
        private void EnsureConnected()
        {
            if (_master == null)
            {
                throw new InvalidOperationException("请先打开串口");
            }
        }

        /// <summary>
        /// 读保持寄存器（功能码 0x03）。
        /// </summary>
        /// <param name="slaveId">从站号</param>
        /// <param name="startAddress">起始地址</param>
        /// <param name="numberOfPoints">读取数量</param>
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
        /// 实现 <see cref="IDisposable"/>，释放串口资源。
        /// </summary>
        public void Dispose()
        {
            Close();
            _serialPort?.Dispose();
            _serialPort = null;
        }
    }
}