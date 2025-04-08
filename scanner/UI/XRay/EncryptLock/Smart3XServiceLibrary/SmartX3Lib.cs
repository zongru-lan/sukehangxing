using SmartX3.Net.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Timers;
using UI.XRay.Common.Utilities;

namespace UI.XRay.EncryptLock.Smart3XServiceLibrary
{
    public class SmartX3Service
    {

        public static SmartX3Service Service { get; private set; }

        static SmartX3Service()
        {
            Service = new SmartX3Service();
        }

        private object _usbLock = new object();
        private static ReturnLockState returnState; //对外发送的状态
        private long keyHandle;//加密锁标识

        protected SmartX3Service()
        {
            returnState = ReturnLockState.OK;
            ServiceRunning = false;
            UsedHours = 0;
        }

        //发送当前次数
        public event EventHandler<Smart3XEventArgs> UsedHoursWeakEvent
        {
            add { _usedHoursWeakEvent.Add(value); }
            remove { _usedHoursWeakEvent.Remove(value); }
        }

        private SmartWeakEvent<EventHandler<Smart3XEventArgs>> _usedHoursWeakEvent =
            new SmartWeakEvent<EventHandler<Smart3XEventArgs>>();

        /// <summary>
        /// 用于主动发送加密锁状态，用于关机等操作
        /// </summary>
        public event EventHandler<ReturnLockState> SendLockStateWeakEvent
        {
            add { _sendLockStateWeakEvent.Add(value); }
            remove { _sendLockStateWeakEvent.Remove(value); }
        }
        private SmartWeakEvent<EventHandler<ReturnLockState>> _sendLockStateWeakEvent =
            new SmartWeakEvent<EventHandler<ReturnLockState>>();

        private uint _usedHours = 0;
        /// <summary>
        /// 当前加密锁记录次数
        /// </summary>
        public uint UsedHours
        {
            get { return _usedHours; }
            private set { _usedHours = value; }
        }

        private bool _isNewLock;
        /// <summary>
        /// 是否是新加密锁，标志位0位
        /// </summary>
        public bool IsNewLock
        {
            get { return _isNewLock; }
            private set { _isNewLock = value; }
        }


        /// <summary>
        /// 0-正常；1-没找到加密锁；2-读数据失败；3-读数据时可执行文件错误；4-写数据失败；5-写数据时可执行文件错误；
        /// </summary>
        /// <returns></returns>
        public ReturnLockState InitUsbLock(out string keyUID)
        {
            keyUID = "";
            string OriginalStr = "lg/qAmVBvPTSixUvV5TreRH8SuySs0fe/i8ts0XBKH65IFdr8jecfw==";
            string usbKeyID = UI.XRay.EncryptLock.Smart3XServiceLibrary.SoftLocker.Decrypt(OriginalStr);
            keyHandle = FindSmartX3(usbKeyID,out keyUID);//读取加密锁标识
            if (keyHandle == 0)
            {
                //加密锁没找到
                returnState = ReturnLockState.NoLock;
            }
            else
            {
                lock (_usbLock)
                {
                    IsNewLock = ReadData(keyHandle, 0, 1) == 1 ? true : false;
                    //读取然后写入，测试加密锁
                    UsedHours = ReadData(keyHandle, 2, 6);
                }

            }
            return returnState;
        }

         [MethodImpl(MethodImplOptions.Synchronized)]
        private static long FindSmartX3(string keyID,out string keyUID)
        {
            keyUID = "";
            long Rtn = 0;
            long[] keyHandles = new long[8];
            long keyNumber = 0;

            string getUid = string.Empty;
            Rtn = SmartX3App.SmartX3Find(keyID, out keyHandles, ref keyNumber);
            if (Rtn != 0)
            {
                return 0;
            }
            else
            {
                SmartX3App.SmartX3GetUid(keyHandles[0], ref getUid);
                keyUID = getUid;
                return keyHandles[0];
            }
        }

         [MethodImpl(MethodImplOptions.Synchronized)]
        private static uint ReadData(long keyHandle, UInt32 startIdx, UInt32 length)
        {
            Int32 resultValue = 0;
            long Rtn = 0;
            UInt32 mode = 1; //模式，0为写，1为读
            UInt32 readAdress = startIdx;
            byte[] readBuffer = new byte[8];
            UInt32 len = length;

            SmartX3App.SmartX3SetVarValue(0, mode);
            SmartX3App.SmartX3SetVarValue(1, readAdress);
            SmartX3App.SmartX3SetVarValue(2, len);
            SmartX3App.SmartX3SetVarValue(3, readBuffer, 8);


            Rtn = SmartX3App.SmartX3Execute(keyHandle, 0, ref resultValue);

            if (Rtn != 0)
            {
                //读取错误
                returnState = ReturnLockState.ReadError;
                return 0;
            }
            else
            {
                if (resultValue != 0)
                {
                    //可执行文件报错
                    returnState = ReturnLockState.ReadExeError;
                    return 0;
                }
                else
                {
                    SmartX3App.SmartX3GetVarResult(3, ref readBuffer);
                    string readedData = System.Text.Encoding.GetEncoding("gb2312").GetString(readBuffer);
                    SmartX3App.SmartX3ClearVar();
                    return uint.Parse(readedData);
                }
            }
        }

         [MethodImpl(MethodImplOptions.Synchronized)]
        private static void WriteData(long keyHandle, UInt32 startIdx, string data)
        {
            Int32 resultValue = 0;
            long Rtn = 0;
            UInt32 mode = 0; //模式，0为写，1为读
            UInt32 writeAdress = startIdx;

            byte[] writeBuffer = new byte[data.Length];
            UInt32 wrietLen = (uint)data.Length; //长度为写入数据的长度

            //将页面的值转为Byte[] 类型
            byte[] bufferNow = System.Text.Encoding.Default.GetBytes(data);
            Array.Copy(bufferNow, writeBuffer, bufferNow.Length);

            //绑定参数
            SmartX3App.SmartX3SetVarValue(0, mode);
            SmartX3App.SmartX3SetVarValue(1, writeAdress);
            SmartX3App.SmartX3SetVarValue(2, wrietLen);
            SmartX3App.SmartX3SetVarValue(3, writeBuffer, 8);

            try
            {
                //执行可执行文件
                Rtn = SmartX3App.SmartX3Execute(keyHandle, 0, ref resultValue);
            }
            catch (Exception)
            {
                Rtn = 1;
            }

            if (Rtn != 0)
            {
                //MessageBox.Show("Failed to write data to USB key!", "Error");
                //写入文件报错
                returnState = ReturnLockState.WriteError;
                return;
            }
            else
            {
                if (resultValue != 0)
                {
                    //MessageBox.Show("An error ocurred in executable file of USB key when write data!", "Error");
                    //可执行文件报错
                    returnState = ReturnLockState.WriteExeError;
                    return;
                }
                else
                {
                    SmartX3App.SmartX3ClearVar();
                }
            }
        }

        private Timer _tickTimer;
        public bool ServiceRunning { get; set; }
        public void StartService()
        {
            ServiceRunning = true;
            if (_tickTimer == null)
            {
                _tickTimer = new Timer(1000 * 60 * 60);
                _tickTimer.Enabled = true;
                _tickTimer.Elapsed += _tickTimer_Elapsed;
                _tickTimer.Start();
            }
            RemainderHousAdd1();
        }

        public void StopService()
        {
            ServiceRunning = false;
            if (_tickTimer != null)
            {
                _tickTimer.Elapsed -= _tickTimer_Elapsed;
                _tickTimer.Stop();
            }
        }

        /// <summary>
        /// 剩余时间减一
        /// </summary>
        private void RemainderHousAdd1()
        {
            RemainderHoursMinusAndSave();
        }

        private void _tickTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            RemainderHoursMinusAndSave();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void RemainderHoursMinusAndSave()
        {
            lock (_usbLock)
            {
                _usedHours = ReadData(keyHandle, 2, 6);
                _usedHours++;
                string toWriteData = _usedHours.ToString().PadLeft(6, '0');
                WriteData(keyHandle, 2, toWriteData);
            }
            _usedHoursWeakEvent.Raise(this, new Smart3XEventArgs(_usedHours));
        }
    }

    public class Smart3XEventArgs : EventArgs
    {
        public uint RemainderHours { get; set; }

        public Smart3XEventArgs(uint remainHours)
        {
            RemainderHours = remainHours;
        }
    }

    public enum ReturnLockState
    {
        OK = 0,
        NoLock = 1,
        ReadError = 2,
        ReadExeError = 3,
        WriteError = 4,
        WriteExeError = 5
    }
}
