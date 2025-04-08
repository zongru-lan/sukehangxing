using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XRayNetEntities.Models;
using FreeSql;
using UI.XRay.Flows.TRSNetwork.Models;

namespace UI.XRay.Flows.TRSNetwork.Tools
{
    public class FreeSqlHelper
    {
        static readonly string connString = Global.Instance.App.DBString;


        public static IFreeSql ISql
        {
            get
            {
                if (iSql == null)
                {
                    iSql = new FreeSqlBuilder()
                        .UseConnectionString(DataType.SqlServer, connString)
                        .UseMonitorCommand(c => Console.WriteLine(c.CommandText))
                        .UseAutoSyncStructure(false)
                        .Build();
                }
                return iSql;
            }
        }

        private static IFreeSql iSql;
    }
}
