using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UI.Common.Tracers
{
    #region 日志等级
    public enum LogLevel
    {
        DebugLog = 0,
        TraceLog,
        InfoLog,
        ErrorLog
    }
    #endregion

    public class Tracer
    {
        #region 变量属性
        static private int nLogId = 0;
        static private string strLogDirPath = "D:\\SecurityScanner\\Logs";
        //static private string strLogDirPath = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        static private string strLogFileName = "HwXrayScanner.log";
        #endregion

        #region 外部接口函数 
        [DllImport("HwiLOG.dll", EntryPoint = "HW_Create_DT_Log_Instances", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private extern static void Create(int Count);


        [DllImport("HwiLOG.dll", EntryPoint = "HW_Open_DT_Log", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private extern static int Open(int Index, string LogRootDir, string LogFileName);


        [DllImport("HwiLOG.dll", EntryPoint = "HW_Set_DT_Log_Cache_Size", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private extern static void SetCacheSize(int Index);


        [DllImport("HwiLOG.dll", EntryPoint = "HW_Is_DT_Log_Opened", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private extern static int IsOpened(int Index);


        [DllImport("HwiLOG.dll", EntryPoint = "HW_Is_DT_Log_Idle", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private extern static int IsIdle(int Index);

        [DllImport("HwiLOG.dll", EntryPoint = "HW_Flush_DT_Log", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private extern static int Flush(int Index, string Text);

        [DllImport("HwiLOG.dll", EntryPoint = "HW_Close_DT_Log", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private extern static void Close(int Index);

        [DllImport("HwiLOG.dll", EntryPoint = "HW_Destroy_DT_Log_Instances", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private extern static void Destroy();
        #endregion

        #region 本地接口

        public static bool Initialize()
        {
            Tracer.Create(nLogId + 1);
            DirectoryInfo directoryInfo = new DirectoryInfo(strLogDirPath);
            if (!directoryInfo.Exists)
            {
                directoryInfo.Create();
            }
            int nResult = Tracer.Open(nLogId, directoryInfo.FullName, strLogFileName);
            return nResult == 1;
        }

        public static bool UnInitialize()
        {                
            Tracer.Close(nLogId + 1);
            Tracer.Destroy();
            return true;
        }
        public static void TraceLog(LogLevel nLevel, string message)
        {
            switch (nLevel)
            {
                case LogLevel.DebugLog:
                    Tracer.Flush(nLogId, "DEBUG " + message);
                    break;
                case LogLevel.TraceLog:
                    Tracer.Flush(nLogId, "TRACE " + message);
                    break;
                case LogLevel.InfoLog:
                    Tracer.Flush(nLogId, "INFO " + message);
                    break;
                case LogLevel.ErrorLog:
                    Tracer.Flush(nLogId, "ERROR " + message);
                    break;
            }
        }

        public static void TraceException(Exception exception, string message = null)
        {
            Tracer.Flush(nLogId, "Exception " + exception.ToString());
        }
        public static void TraceDebug(string message, params object[] parameters)
        {
            Tracer.Flush(nLogId, "Debug " + message + getObjStrings(parameters));
        }
        public static void TraceEnterFunc(string functionName)
        {
            Tracer.Flush(nLogId, "EnterFuc " + functionName);
        }
        public static void TraceEnterFunc(string functionName, params object[] parameters)
        {
            Tracer.Flush(nLogId, "EnterFuc " + functionName + getObjStrings(parameters));
        }
        public static void TraceError(string errorMessage, int errorNumber = 0)
        {
            Tracer.Flush(nLogId, "Error " + errorMessage);
        }
        public static void TraceExitFunc(string functionName)
        {
            Tracer.Flush(nLogId, "ExitFuc " + functionName);
        }
        public static void TraceExitFunc(string functionName, params object[] parameters)
        {
            Tracer.Flush(nLogId, "ExitFuc " + functionName + getObjStrings(parameters));
        }
        public static void TraceInfo(string message, params object[] parameters)
        {
            Tracer.Flush(nLogId, "Info " + message + getObjStrings(parameters));
        }
        public static void TraceWarning(string message, params object[] parameters)
        {

            Tracer.Flush(nLogId, "Warning " + message + getObjStrings(parameters));
        }

        private static string getObjStrings(params object[] parameters)
        {
            string strObj = "";
            foreach (Object param in parameters)
            {
                strObj += param.ToString() + ",";
            }
            return strObj;
        }
        #endregion
    }
}


