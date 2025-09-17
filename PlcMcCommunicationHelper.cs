using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HslCommunication;
using System.Diagnostics;

namespace Labeller
{
    /* 三菱PLC通讯类，采用Qna兼容3E帧协议实现，需要在PLC侧先对以太网模块进行配置，必须为二进制通讯
     * 需在PLC侧“内置以太网端口设置”中设定IP、端口号，选择TCP协议，选择MC协议，
     * 选择“通信数据代码设置”为二进制码通信，对“允许RUN中写入(FTP与MC协议)”打钩 */

    /* MC协议的目的是开放PLC内部寄存器给外部设备，实现外部设备和PLC的数据交互。简单说，就是允许MC协议来读写PLC里面的寄存器
     * 通讯方式有485和TCP/IP，这里描述的是TCP/IP的通讯方法
     * 通讯内容分为二进制和ASCII文本，两者传输内容一致，只是形式不同。
     * 因为二进制相对于ASCII码形式，一帧的数据长度更短，且数据不需要转换，所以通讯效率更高，推荐使用二进制形式*/


    /* PLC的M寄存器实际上是辅助继电器，只有0和1两种状态；D寄存器是16位的数值存储器 */

    public enum HeartBeatState
    {
        Activated,
        Unactivated,
    }

    public class PlcMcCommunicationHelper
    {
        #region Singleton Instance
        private static PlcMcCommunicationHelper singletonPlcCommunicator = null;
        public static PlcMcCommunicationHelper SetPlcMcCommunicationHelper(string ipAddress, int port)
        {
            if (singletonPlcCommunicator == null)
            {
                singletonPlcCommunicator = new PlcMcCommunicationHelper(ipAddress, port);
            }
            return singletonPlcCommunicator;
        }
        public static PlcMcCommunicationHelper GetPlcMcCommunicationHelper()
        {
            if (singletonPlcCommunicator == null)
            {
                throw new ArgumentException("Plc Communicator can not be null", "Plc Communicator");
            }
            return singletonPlcCommunicator;
        }
        #endregion
        
        public bool IsConnected { get; set; }
        public HeartBeatState CurrentHeartBeatState { get; private set; } = HeartBeatState.Unactivated;

        private string ipAddress = null;
        private int port = 0;
        private HslCommunication.Profinet.Melsec.MelsecMcNet communicator = null;

        private string heartBeatAddressIn = "M11040";//心跳
        private string heartBeatAddressOut = "M11090";//回复心跳
        private System.Timers.Timer tmrMonitor = null;
        private Stopwatch sw = null;
        private int commingCount = 0;
        private bool bCurrentHighLevel = false;
        private Stopwatch sw1 = new Stopwatch();


        private PlcMcCommunicationHelper(string ipAddress, int port)
        {
            this.ipAddress = ipAddress;
            this.port = port;

            this.tmrMonitor = new System.Timers.Timer(500);
            this.tmrMonitor.AutoReset = false;
            this.tmrMonitor.Elapsed += UpdateHeartBeatState;
            this.tmrMonitor.Start();
            this.sw = new Stopwatch();
        }

        public void SetIpAddressAndPort(string ipAddress, int port)
        {
            this.ipAddress = ipAddress;
            this.port = port;
        }

        public bool Connect()
        {
            communicator = new HslCommunication.Profinet.Melsec.MelsecMcNet(ipAddress, port);
            communicator.ConnectTimeOut = 3000;
            OperateResult connectResult = communicator.ConnectServer();
            IsConnected = connectResult.IsSuccess;

            if (IsConnected) { tmrMonitor.Start(); }

            return IsConnected;
        }

        public bool Close()
        {
            OperateResult closeResult = communicator.ConnectClose();
            IsConnected = !closeResult.IsSuccess;

            if (!IsConnected) { tmrMonitor.Stop(); }

            return IsConnected;
        }

        [Obsolete("无法适用于值类型的读取")]
        public bool Read<T>(string address, out T result) where T : class, new()
        {
            bool bOperateSuccessful = false;
            switch (typeof(T).ToString())
            {
                case "System.Int16":
                    OperateResult<short> resultShort = communicator.ReadInt16(address);
                    bOperateSuccessful = resultShort.IsSuccess;
                    result = resultShort.Content as T;
                    break;
                case "System.Int32":  //int
                    OperateResult<int> resultInt = communicator.ReadInt32(address);
                    bOperateSuccessful = resultInt.IsSuccess;
                    result = resultInt.Content as T;
                    break;
                case "System.Int64":  //long
                    OperateResult<long> resultLong = communicator.ReadInt64(address);
                    bOperateSuccessful = resultLong.IsSuccess;
                    result = resultLong.Content as T;
                    break;
                case "System.Double":  //double
                    OperateResult<double> resultDouble = communicator.ReadDouble(address);
                    bOperateSuccessful = resultDouble.IsSuccess;
                    result = resultDouble.Content as T;
                    break;
                case "System.Boolean":  //bool
                    OperateResult<bool> resultBool = communicator.ReadBool(address);
                    bOperateSuccessful = resultBool.IsSuccess;
                    result = resultBool.Content as T;
                    break;
                default:
                    throw new ArgumentException("the type of this Param is not defined：[ " + typeof(T).ToString() + " ]", "Param Type Error");
            }

            return bOperateSuccessful;

        }

        public bool ReadShortData(string address, out short readData)
        {
            OperateResult<short> resultShort = communicator.ReadInt16(address);
            readData = resultShort.Content;
            return resultShort.IsSuccess;
        }
        public bool ReadIntData(string address, out int readData)
        {
            OperateResult<int> resultInt = communicator.ReadInt32(address);
            readData = resultInt.Content;
                
            return resultInt.IsSuccess;
        }
        public bool ReadLongData(string address, out long readData)
        {
            OperateResult<long> resultLong = communicator.ReadInt64(address);
            readData = resultLong.Content;
            return resultLong.IsSuccess;
        }
        public bool ReadDoubleData(string address, out double readData)
        {
            OperateResult<double> resultDouble = communicator.ReadDouble(address);
            readData = resultDouble.Content;
            return resultDouble.IsSuccess;
        }
        public bool ReadBoolData(string address, out bool readData)
        {
            OperateResult<bool> resultBool = communicator.ReadBool(address);
            readData = resultBool.Content;
                
            return resultBool.IsSuccess;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">short、int、long、double、bool、string</typeparam>
        /// <param name="address">起始地址</param>
        /// <param name="length">读取长度</param>
        /// <returns></returns>
        public T[] Read<T>(string address, int length)
        {
            T[] ts;
            switch (typeof(T).ToString())
            {
                case "System.Int16":  //short[]
                    OperateResult<short[]> resultShort = communicator.ReadInt16(address, Convert.ToUInt16(length));
                    ts = resultShort.Content as T[];
                    break;
                case "System.Int32":  //int[]
                    OperateResult<int[]> resultInt = communicator.ReadInt32(address, Convert.ToUInt16(length));
                    ts = resultInt.Content as T[];
                    break;
                case "System.Int64":  //long[]
                    OperateResult<long[]> resultLong = communicator.ReadInt64(address, Convert.ToUInt16(length));
                    ts = resultLong.Content as T[];
                    break;
                case "System.Double":  //double[]
                    OperateResult<double[]> resultDouble = communicator.ReadDouble(address, Convert.ToUInt16(length));
                    ts = resultDouble.Content as T[];
                    break;
                case "System.Boolean":  //bool[]
                    OperateResult<bool[]> resultBool = communicator.ReadBool(address, Convert.ToUInt16(length));
                    ts = resultBool.Content as T[];
                    break;
                case "System.String":  //string
                    OperateResult<string> resultString = communicator.ReadString(address, Convert.ToUInt16(length));
                    ts = resultString.Content as T[];
                    break;
                default:
                    throw new ArgumentException("the type of this Param is not defined：[ " + typeof(T).ToString() + " ]", "Param Type Error");
            }

            return ts;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">short、int、long、double、bool、string</typeparam>
        /// <param name="address">起始地址</param>
        /// <param name="content">写入内容</param>
        /// <returns></returns>
        public bool Write<T>(string address, T content)
        {
            
            OperateResult operateResult;
            switch (typeof(T).ToString())
            {
                case "System.Int16":  //short
                    operateResult = communicator.Write(address, Convert.ToInt16(content));
                    break;
                case "System.Int32":  //int
                    operateResult = communicator.Write(address, Convert.ToInt32(content));
                    break;
                case "System.Int64":  //long
                    operateResult = communicator.Write(address, Convert.ToInt64(content));
                    break;
                case "System.Double":  //double
                    operateResult = communicator.Write(address, Convert.ToDouble(content));
                    break;
                case "System.Boolean":  //bool
                    operateResult = communicator.Write(address, Convert.ToBoolean(content));
                    break;
                case "System.String":  //string
                    operateResult = communicator.Write(address, Convert.ToString(content));
                    break;
                default:
                    throw new ArgumentException("the type of this Param is not defined：[ " + typeof(T).ToString() + " ]", "Param Type Error");
            }
                
            return operateResult.IsSuccess;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">int、long、double、bool</typeparam>
        /// <param name="address">起始地址</param>
        /// <param name="contents">写入内容</param>
        /// <returns></returns>
        public bool Write<T>(string address, T[] contents)
        {
            OperateResult operateResult;
            switch (typeof(T).ToString())
            {
                case "System.Int16":  //short
                    short[] writeShort = Array.ConvertAll<T, short>(contents, temp => { return Convert.ToInt16(temp); });
                    operateResult = communicator.Write(address, writeShort);
                    break;
                case "System.Int32":  //int
                    int[] writeInt = Array.ConvertAll<T, int>(contents, temp => { return Convert.ToInt32(temp); });
                    operateResult = communicator.Write(address, writeInt);
                    break;
                case "System.Int64":  //long
                    long[] writeLong = Array.ConvertAll<T, long>(contents, temp => { return Convert.ToInt64(temp); });
                    operateResult = communicator.Write(address, writeLong);
                    break;
                case "System.Double":  //double
                    double[] writeDouble = Array.ConvertAll<T, double>(contents, temp => { return Convert.ToDouble(temp); });
                    operateResult = communicator.Write(address, writeDouble);
                    break;
                case "System.Boolean":  //bool
                    bool[] writeBool = Array.ConvertAll<T, bool>(contents, temp => { return Convert.ToBoolean(temp); });
                    operateResult = communicator.Write(address, writeBool);
                    break;
                default:
                    throw new ArgumentException("the type of this Param is not defined：[ " + typeof(T).ToString() + " ]", "Param Type Error");
            }

            return operateResult.IsSuccess;
        }

        private void UpdateHeartBeatState(object sender, System.Timers.ElapsedEventArgs e)
        {
            return;
            try
            {
                this.tmrMonitor.Stop();

                if (!IsConnected)
                {
                    CurrentHeartBeatState = HeartBeatState.Unactivated;
                    return;
                }

                //Control Myself Heartbeat
                if (++commingCount >= 2)
                {
                    long time = sw1.ElapsedMilliseconds;
                    sw1.Restart();
                    sw.Start();
                    commingCount = 0;
                    bool pcHeartBeat = communicator.ReadBool(heartBeatAddressOut).Content;
                    communicator.Write(heartBeatAddressOut, !pcHeartBeat);
                }

                //Monitor PLC Heartbeat
                if (sw.IsRunning)
                {
                    bool nowLevel = communicator.ReadBool(heartBeatAddressIn).Content;
                    if (bCurrentHighLevel != nowLevel)
                    {
                        bCurrentHighLevel = nowLevel;
                        CurrentHeartBeatState = HeartBeatState.Activated;
                        sw.Restart();
                    }
                    else
                    {
                        if (sw.ElapsedMilliseconds > 3000)
                        {
                            CurrentHeartBeatState = HeartBeatState.Unactivated;
                            sw.Restart();
                        }
                    }
                }
                else { sw.Start(); }
            }
            catch { }
            finally { this.tmrMonitor.Start(); }
            
        }

    }
}
