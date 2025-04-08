using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows;
using UI.Common.Tracers;
using UI.XRay.Gui.Framework;

namespace UI.XRay.Security.Configer
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        System.Threading.Mutex mutex;  
        public App()
        {
            try
            {
                TranslationService.LoadLanguageFile();
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                Process thisProc = Process.GetCurrentProcess();
                if (Process.GetProcessesByName(thisProc.ProcessName).Length > 1)
                {
                    Application.Current.Shutdown();
                    return;
                }

                if (e.Args.Length == 1 && e.Args[0] == "--dbupdate")
                {
                    // 更新数据库
                    Tracer.TraceInfo("Update Database in Configer");
                    UpdateGroupFunc(@"D:/SecurityScanner/Database/scanner.db");
                }

                //判断命令参数个数，如若为2则正确，处理命令，并退出程序；若为0，则正常启动程序，进入主窗口；其它则错误，直接退出程序
                if (e.Args.Length == 2)
                {
                    if (e.Args[0] == "-i")
                    {
                        //导入命令处理
                        Tracer.TraceInfo("Importing configurations from xml file.", e.Args[1]);
                        ConfigImportExportHelper.Import(e.Args[1]);
                    }
                    else if (e.Args[0] == "-o")
                    {
                        //导出命令处理
                        Tracer.TraceInfo("Export configurations to xml file:", e.Args[1]);
                        ConfigImportExportHelper.Export(e.Args[1]);
                    }
                    else
                    {
                        Tracer.TraceError("Input args is wrong:" + e.Args[0]);
                    }

                    //退出应用程序
                    Shutdown();
                }
                else if (e.Args.Length != 0)
                {
                    Tracer.TraceError("Input args count is not correct. " + e.Args.Length);
                    Shutdown();
                }

                base.OnStartup(e);
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception);
            }
        }

        public void UpdateGroupFunc(string dbPath)
        {
            SQLiteConnection conn = new SQLiteConnection($"DATA SOURCE={dbPath}");
            if (conn.State != System.Data.ConnectionState.Open)
            {
                conn.Open();
                SQLiteCommand cmd = new SQLiteCommand(conn);
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master where type='table' and name='AccountGroup'";
                var isAccountGroupExist = Convert.ToInt32(cmd.ExecuteScalar());
                if (isAccountGroupExist == 0)
                {
                    cmd.CommandText = "CREATE TABLE AccountGroup (GroupID VARCHAR PRIMARY KEY NOT NULL, GroupName VARCHAR NOT NULL, Description TEXT DEFAULT '')";
                    cmd.ExecuteNonQuery();
                    Tracer.TraceInfo("[SQLiteHelper] Created table AccountGroup");
                }
                else
                {
                    Tracer.TraceInfo("[SQLiteHelper] Table AccountGroup exists");
                }

                cmd.CommandText = "SELECT * FROM sqlite_master where name='Account' and sql like '%IsNetAccount%'";
                var isNetAccountExist = cmd.ExecuteScalar();
                if (isNetAccountExist == null)
                {
                    cmd.CommandText = "ALTER TABLE Account ADD COLUMN IsNetAccount BOOL NOT NULL DEFAULT 0";
                    cmd.ExecuteNonQuery();
                    Tracer.TraceInfo("[SQLiteHelper] Created field IsNetAccount in table Account");
                }
                else
                {
                    Tracer.TraceInfo("[SQLiteHelper] Field IsNetAccount exists in table Account");
                }

                cmd.CommandText = "SELECT * FROM sqlite_master where name='Account' and sql like '%GroupName%'";
                var isGroupNameExist = cmd.ExecuteScalar();
                if (isGroupNameExist == null)
                {
                    cmd.CommandText = "ALTER TABLE Account ADD COLUMN GroupName VARCHAR NOT NULL DEFAULT ''";
                    cmd.ExecuteNonQuery();
                    Tracer.TraceInfo("[SQLiteHelper] Created field GroupName in table Account");
                }
                else
                {
                    Tracer.TraceInfo("[SQLiteHelper] Field GroupName exists in table Account");
                }

                cmd.CommandText = "SELECT * FROM sqlite_master where name='Account' and sql like '%IsEnable%'";
                var isEnableExist = cmd.ExecuteScalar();
                if (isEnableExist == null)
                {
                    cmd.CommandText = "ALTER TABLE Account ADD COLUMN IsEnable BOOL NOT NULL DEFAULT 1 ";
                    cmd.ExecuteNonQuery();
                    Tracer.TraceInfo("[SQLiteHelper] Created field IsEnable in table Account");
                }
                else
                {
                    Tracer.TraceInfo("[SQLiteHelper] Field IsEnable exists in table Account");
                }

                cmd.CommandText = "SELECT * FROM sqlite_master where name='Account' and sql like '%EffectsCompositions%'";
                var isEffectsCompositionsExist = cmd.ExecuteScalar();
                if (isEffectsCompositionsExist == null)
                {
                    cmd.CommandText = "ALTER TABLE Account ADD COLUMN EffectsCompositions VARCHAR NOT NULL DEFAULT 'MaterialColor,SlicePenetrate,False,False;MaterialColor,SlicePenetrate,False,False;MaterialColor,SlicePenetrate,False,False'";
                    cmd.ExecuteNonQuery();
                    Tracer.TraceInfo("[SQLiteHelper] Created field EffectsCompositions in table Account");
                }
                else
                {
                    Tracer.TraceInfo("[SQLiteHelper] Field EffectsCompositions exists in table Account");
                }


                cmd.CommandText = "SELECT * FROM sqlite_master where name='Account' and sql like '%ActionTypes%'";
                var isActionTypesExist = cmd.ExecuteScalar();
                if (isActionTypesExist == null)
                {
                    cmd.CommandText = "ALTER TABLE Account ADD COLUMN ActionTypes VARCHAR NOT NULL DEFAULT 'StartShapeCorrection;StartShapeCorrection;StartShapeCorrection'";
                    cmd.ExecuteNonQuery();
                    Tracer.TraceInfo("[SQLiteHelper] Created field ActionTypes in table Account");
                }
                else
                {
                    Tracer.TraceInfo("[SQLiteHelper] Field ActionTypes exists in table Account");
                }
            }
            conn.Close();
        }
    }
}
