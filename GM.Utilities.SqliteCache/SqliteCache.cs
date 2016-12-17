using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace GM.Utilities.SqliteCache
{
    public class SqliteCache : ISqliteCache
    {
        private static SqliteCache _instance = null;
        private static readonly object _sync = new object();
        /// <summary>
        /// SqliteCache实例
        /// </summary>
        public static SqliteCache Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_sync)
                    {
                        if (_instance == null)
                        {
                            _instance = new SqliteCache();
                        }
                    }
                }
                return _instance;
            }
        }
        /// <summary>
        /// 数据缓存队列
        /// </summary>
        private List<QueueGroup> _queueGroupList;
        /// <summary>
        /// 从sqlite读数据的时间间隔
        /// </summary>
        private int _readInterval = SqliteCacheSetting.Instance.ReadThreadInterval * 1000;
        /// <summary>
        /// 向sqlite写数据的时间间隔
        /// </summary>
        private int _writeInterval = SqliteCacheSetting.Instance.WriteThreadInterval * 1000;

        private Thread _readThread;
        /// <summary>
        /// sqlite数据库文件存储目录
        /// </summary>
        private readonly string _sqliteDbDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SqliteDbFile");
        /// <summary>
        /// sqlite数据库文件备份目录
        /// </summary>
        private readonly string _sqliteDbBakPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SqliteDbBak");
        /// <summary>
        /// 记录sqlite断点的数据库
        /// </summary>
        private const string SqliteRecordFileName = "SqliteRecord.db";
        /// <summary>
        /// 记录sqlite断点的表名
        /// </summary>
        private const string SqliteRecordTableName = "READ_RECORD";
        /// <summary>
        /// 创建记录sqlite断点的数据库表脚本
        /// </summary>
        private string _createRecordTableSql = @"CREATE TABLE READ_RECORD
                    (
                      Id                  integer PRIMARY KEY autoincrement, 
                      DBFILE_PATH         VARCHAR2(50),
                      TABLE_NAME          VARCHAR2(50),
                      RECORD_ID           NUMBER(9),    --未读的表 0，读完的表 -1，读取中的表 正常Id
                      CLASS_TYPE          VARCHAR2(50),
                      UPDATE_TIME         DATE              DEFAULT (datetime('now', 'localtime'))
                    )";
        /// <summary>
        /// 表名称和类型字典
        /// </summary>
        private Dictionary<string, Type> _typeDic = new Dictionary<string, Type>();

        private SqliteCache() { }

        private int _entityIndex = 0;
        /// <summary>
        /// 添加数据对象
        /// </summary>
        /// <param name="entity"></param>
        public void Add(SqliteEntity entity)
        {
            try
            {
                if (_queueGroupList == null || _queueGroupList.Count < 1)
                {
                    throw new ApplicationException("无法添加数据，队列未正确初始化");
                }
                Type type = entity.GetType();
                string tableName = Class2DbHelper.Instance[type].ClassAttribute.TableName;
                if (!_typeDic.ContainsKey(tableName))
                {
                    _typeDic[tableName] = type;
                }
                int queueIndex = _entityIndex % _queueGroupList.Count;
                if (!_queueGroupList[queueIndex].QueueDic.ContainsKey(type))
                {
                    _queueGroupList[queueIndex].QueueDic[type] = new Queue();
                }
                _queueGroupList[queueIndex].QueueDic[type].Enqueue(entity);
                _entityIndex++;
                if (_entityIndex == _queueGroupList.Count)
                {
                    _entityIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteErrorFmt(Log.AddEntity, "Sqlite添加数据对象时出现异常，异常信息：{0}", ex);
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="queueCount"></param>
        public void Init(int queueCount)
        {
            try
            {
                //初始化队列
                _queueGroupList = new List<QueueGroup>();
                for (int i = 0; i < queueCount; i++)
                {
                    _queueGroupList.Add(new QueueGroup() { QueueDic = new Dictionary<Type, Queue>() });
                }
                CheckDbFiles();
                //初始化读写线程
                for (int i = 0; i < queueCount; i++)
                {
                    var writeProcessor = new WriteProcessor()
                                             {
                                                 QueueIndex = i,
                                                 WriteInterval = _writeInterval,
                                                 DbPath = _sqliteDbDirectoryPath
                                             };
                    Thread t = new Thread(writeProcessor.WriteSqlite);
                    t.Start(_queueGroupList[i]);
                }
                var readProcessor = new ReadProcessor()
                                        {
                                            ReadInterval = _readInterval,
                                            DbPath = _sqliteDbDirectoryPath,
                                            DbBakPath = _sqliteDbBakPath,
                                            RecordDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SqliteRecordFileName),
                                            RecordTableName = SqliteRecordTableName
                                        };
                _readThread = new Thread(readProcessor.ReadSqlite);
                _readThread.Start(_typeDic);
            }
            catch (Exception ex)
            {
                Logger.WriteErrorFmt(Log.SqliteCacheInit, "Sqlite初始化时出现异常，异常信息：{0}", ex);
            }
        }

        /// <summary>
        /// sqlite数据库文件检测
        /// </summary>
        private void CheckDbFiles()
        {
            if (!Directory.Exists(_sqliteDbDirectoryPath))
            {
                Directory.CreateDirectory(_sqliteDbDirectoryPath);
            }
            else
            {
                string[] fileNames = Directory.GetFiles(_sqliteDbDirectoryPath);
                for (int i = 0; i < fileNames.Length; i++)
                {
                    //删掉*.db-journal文件
                    if (fileNames[i].Contains(".db-journal"))
                    {
                        File.Delete(Path.Combine(_sqliteDbDirectoryPath, fileNames[i]));
                        continue;
                    }
                    //xxx.write.db文件替换为xxx.read.db文件
                    if (fileNames[i].Contains(".write."))
                    {
                        string readFileName = fileNames[i].Replace("write", "read");
                        File.Move(Path.Combine(_sqliteDbDirectoryPath, fileNames[i]), Path.Combine(_sqliteDbDirectoryPath, readFileName));
                    }
                }
            }
            if (!Directory.Exists(_sqliteDbBakPath))
            {
                Directory.CreateDirectory(_sqliteDbBakPath);
            }
            //检查是否有sqlite读取位置的记录,如果数据库文件和表不存在，则新建
            SqliteDbUtility.Instance.CreateDb(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SqliteRecordFileName), _createRecordTableSql);
        }
    }
}
