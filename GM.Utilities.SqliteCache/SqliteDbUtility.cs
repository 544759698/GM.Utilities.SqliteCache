using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Text;
using GM.Orm.Db;

namespace GM.Utilities.SqliteCache
{
    class SqliteDbUtility
    {
        private static SqliteDbUtility _instance = null;
        public string SqliteConnectionString { get; private set; }
        public SqliteContext sqliteContext;
        private static object _lockObject = new object();

        public static SqliteDbUtility Instance
        {
            get
            {
                lock (_lockObject)
                {
                    if (_instance == null)
                    {
                        _instance = new SqliteDbUtility();
                    }

                    return _instance;
                }
            }
        }

        public void Init(string connectionString)
        {
            sqliteContext = new SqliteContext(new DBContextBridge
            {
                ConnectionString = connectionString,
                DBName = "SqliteCacheDb",
                Provider = "sqlite",
            });
            SqliteConnectionString = connectionString;
        }

        /// <summary>
        /// 创建sqlite数据库文件
        /// </summary>
        public void CreateDb(string databaseFilePath)
        {
            CreateDb(databaseFilePath, string.Empty);
        }

        /// <summary>
        /// 创建数据库
        /// </summary>
        /// <param name="databaseFilePath"></param>
        /// <param name="createTableSql"></param>
        public void CreateDb(string databaseFilePath, string createTableSql)
        {
            if (File.Exists(databaseFilePath))
            {
                Logger.WriteInfoFmt(Log.CreateDb, "检查路径{0}确认sqllite文件存在", databaseFilePath);
            }
            else
            {
                Logger.WriteWarningFmt(Log.CreateDb, "检查路径{0}确认sqllite文件不存在，开始建立sqlite", databaseFilePath);
                File.Create(databaseFilePath).Close();
                SetFileSystemAccessRule(databaseFilePath);
                Logger.WriteInfoFmt(Log.CreateDb, "在{0}创建sqllite文件结束", databaseFilePath);

                if (!string.IsNullOrEmpty(createTableSql))
                {
                    try
                    {
                        string connectionString = string.Concat("Data Source=", databaseFilePath);
                        SqliteContext sqlite = new SqliteContext(new DBContextBridge
                        {
                            ConnectionString = connectionString,
                            Provider = "sqlite",
                        });
                        sqlite.ExecuteSQL("", createTableSql);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteErrorFmt(Log.CreateDb, "创建数据表出现异常，异常信息：{0}", ex);
                    }
                }
            }
        }

        /// <summary>
        /// 设置sqlite数据库权限
        /// </summary>
        /// <param name="fileFullName"></param>
        void SetFileSystemAccessRule(string fileFullName)
        {
            var file = new FileInfo(fileFullName);
            var accControl = file.GetAccessControl();
            accControl.AddAccessRule(new FileSystemAccessRule("everyone", FileSystemRights.FullControl,
                                                              AccessControlType.Allow));
            file.SetAccessControl(accControl);
        }
    }
}
