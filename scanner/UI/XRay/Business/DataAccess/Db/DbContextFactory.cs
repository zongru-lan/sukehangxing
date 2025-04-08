using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.XRay.Business.DataAccess.Db
{
    public static class DbContextFactory
    {
        /// <summary>
        /// 根据指定的数据库类型，获取数据库context
        /// </summary>
        /// <param name="dbType"></param>
        /// <returns></returns>
        public static IScannerDbContextProvider GetContextProvider(string dbType = "Sqlite")
        {
            return new SqliteDbContextProvider();
        }
    }
}
