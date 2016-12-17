using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using GM.Orm;
using GM.Orm.Db;

namespace GM.Utilities.SqliteCache
{
    class WriteProcessor
    {
        /// <summary>
        /// 开始时间
        /// </summary>
        private DateTime? _startTime = null;
        /// <summary>
        /// 向sqlite写数据的时间间隔
        /// </summary>
        public int WriteInterval { get; set; }
        /// <summary>
        /// 队列索引
        /// </summary>
        public int QueueIndex { get; set; }

        /// <summary>
        /// 读写文件目录
        /// </summary>
        public string DbPath { get; set; }
        /// <summary>
        /// 写文件名称
        /// </summary>
        private string _writeFileName = string.Empty;
        /// <summary>
        /// 创建文件的间隔(单位：秒)
        /// </summary>
        private int _dbFileInterval = SqliteCacheSetting.Instance.SqliteFileInterval;
        /// <summary>
        /// 当前sqlite文件的连接
        /// </summary>
        private string _connStr;
        /// <summary>
        /// 数据库中含有的表
        /// </summary>
        private List<string> _tableList = new List<string>();
        /// <summary>
        /// 写sqlite文件
        /// </summary>
        /// <param name="obj"></param>
        public void WriteSqlite(Object obj)
        {
            var queueGroup = (QueueGroup)obj;
            //线程首次启动
            if (_startTime == null)
            {
                //创建文件
                _startTime = DateTime.Now;
                _connStr = InitSqliteDb(_startTime.Value);
            }
            while (true)
            {
                try
                {
                    WriteData(queueGroup);

                    DateTime dtNow = DateTime.Now;
                    if ((dtNow - _startTime.Value).TotalSeconds > _dbFileInterval)
                    {
                        //改名字
                        string readFileName = _writeFileName.Replace("write", "read");
                        File.Move(Path.Combine(DbPath, _writeFileName), Path.Combine(DbPath, readFileName));
                        //新建数据库文件
                        _startTime = dtNow;
                        _connStr = InitSqliteDb(_startTime.Value);
                        _tableList = new List<string>();
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteErrorFmt(Log.WriteSqlite, "写Sqlite数据库时出现异常，异常信息：{0}", ex);
                }

                Thread.Sleep(WriteInterval);
            }
        }

        /// <summary>
        /// 写数据
        /// </summary>
        /// <param name="queueGroup"></param>
        private void WriteData(QueueGroup queueGroup)
        {
            try
            {
                foreach (var key in queueGroup.QueueDic.Keys)
                {//TODO:根据类型获取表名
                    if (!_tableList.Contains(Class2DbHelper.Instance[key].ClassAttribute.TableName))
                    {
                        //创建表
                        CreateTable(key);
                        _tableList.Add(Class2DbHelper.Instance[key].ClassAttribute.TableName);
                    }
                    //插入数据
                    int queueCurrentCount = queueGroup.QueueDic[key].Count;
                    if (queueCurrentCount > 0)
                    {
                        SqliteContext sqliteContext = new SqliteContext(new DBContextBridge
                        {
                            ConnectionString = _connStr,
                            Provider = "sqlite",
                        });
                        int transid = 0;
                        for (int i = 0; i < queueCurrentCount; i++)
                        {
                            if (transid == 0) transid = sqliteContext.RegisteTrans();
                            var entity = queueGroup.QueueDic[key].Dequeue() as SqliteEntity;
                            if (entity != null)
                            {
                                //string opid = entity.GetValue(entity, "ContentOpid").ToString();
                                //string domainId = entity.GetValue(entity, "DomainId").ToString();
                                //string createTime = entity.GetValue(entity, "CreateTime").ToString();
                                //Logger.WriteError("输出GetValue", string.Format("ContentOpid:{0},DomainId:{1},CreateTime:{2}", opid, domainId, createTime));
                                sqliteContext.ExecuteSQL(transid, Class2DbHelper.Instance[key].GetInsertSqliteSql(entity));
                                if ((i > 0 && i % 10000 == 0) || i == queueCurrentCount - 1)
                                {
                                    sqliteContext.TransCommit(transid);
                                    transid = 0;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("写sqlite数据时出现异常", ex);
            }
        }

        /// <summary>
        /// 初始化sqlite数据文件
        /// </summary>
        /// <param name="startTime"></param>
        /// <returns></returns>
        private string InitSqliteDb(DateTime startTime)
        {
            _writeFileName = string.Format("{0}.{1}.write.db", startTime.ToString("yyyyMMddHHmmss"), QueueIndex);
            string databaseFilePath = Path.Combine(DbPath, _writeFileName);
            SqliteDbUtility.Instance.CreateDb(databaseFilePath);
            return string.Concat("Data Source=", databaseFilePath); ;
        }

        /// <summary>
        /// 根据类型创建表
        /// </summary>
        /// <param name="type"></param>
        private void CreateTable(Type type)
        {
            SqliteContext sqliteContext = new SqliteContext(new DBContextBridge
            {
                ConnectionString = _connStr,
                Provider = "sqlite",
            });
            sqliteContext.ExecuteSQL("", Class2DbHelper.Instance[type].GetCreateTableSql());
        }

    }
}
