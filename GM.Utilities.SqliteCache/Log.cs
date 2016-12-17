using System;
using System.Collections.Generic;
using System.Text;

namespace GM.Utilities.SqliteCache
{
    class Log : LogCatalogs
    {
        public static string CreateDb = "创建Sqlite数据库";
        public static string SqliteCacheInit = "SqliteCache初始化";
        public static string AddEntity = "SqliteCache添加数据对象";
        public static string WriteSqlite = "写Sqlite数据库";
        public static string ReadSqlite = "读Sqlite数据库";

        static Log()
        {
            RegisteCatalogs(typeof(Log));
        }
    }
}
