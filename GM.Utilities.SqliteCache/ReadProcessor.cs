using System;
using System.Collections.Generic;
using System.Data;
//using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using GM.Orm.Db;

namespace GM.Utilities.SqliteCache
{
    internal class ReadProcessor
    {
        /// <summary>
        /// 读取数据的时间间隔
        /// </summary>
        public int ReadInterval { get; set; }

        /// <summary>
        /// 读写文件目录
        /// </summary>
        public string DbPath { get; set; }

        /// <summary>
        /// 数据文件备份目录
        /// </summary>
        public string DbBakPath { get; set; }

        /// <summary>
        /// 记录sqlite断点的数据库完整路径
        /// </summary>
        public string RecordDbPath { get; set; }

        /// <summary>
        /// 记录sqlite断点的表名
        /// </summary>
        public string RecordTableName { get; set; }

        //@"CREATE TABLE READ_RECORD
        // (
        //  Id                  integer PRIMARY KEY autoincrement, 
        //  DBFILE_PATH         VARCHAR2(50),
        //  TABLE_NAME          VARCHAR2(50),
        //  RECORD_ID           NUMBER(9),  --未读的表 0，读完的表 -1，读取中的表 正常Id
        //  CLASS_TYPE          VARCHAR2(50),
        //  UPDATE_TIME         DATE              DEFAULT (datetime('now', 'localtime'))
        //  )"

        public void ReadSqlite(Object obj)
        {
            var typeDic = (Dictionary<string, Type>)obj;
            while (true)
            {
                try
                {
                    DataTable dataRecord = null;
                    if (!string.IsNullOrEmpty(RecordTableName))
                        dataRecord = GetSqliteDt("select * from " + RecordTableName,
                                                 string.Concat("Data Source=", RecordDbPath));

                    if (dataRecord != null)
                        ProcessHistoryData(dataRecord, typeDic); //有断点数据
                    else
                        ProcessData(typeDic); //无断点
                }
                catch (Exception ex)
                {
                    Logger.WriteErrorFmt(Log.ReadSqlite, "读Sqlite数据库时出现异常，异常信息：{0}", ex);
                }
                Thread.Sleep(ReadInterval);
            }
        }

        /// <summary>
        /// 处理历史数据（有断点时）
        /// </summary>
        /// <param name="dataRecord"></param>
        /// <param name="typeDic"></param>
        private void ProcessHistoryData(DataTable dataRecord, Dictionary<string, Type> typeDic)
        {
            try
            {
                if (dataRecord.Rows.Count > 0)
                {
                    string dbfilePath = dataRecord.Rows[0]["DBFILE_PATH"].ToString();
                    if (File.Exists(dbfilePath))
                    {
                        List<string> tblist = new List<string>(); //未入库的表
                        string currentTable = string.Empty; //断表
                        int recordId = 0; //断表Id
                        foreach (DataRow row in dataRecord.Rows)
                        {
                            if (Convert.ToInt32(row["RECORD_ID"]) == 0)
                            {
                                tblist.Add(row["TABLE_NAME"].ToString());
                            }
                            if (Convert.ToInt32(row["RECORD_ID"]) > 0)
                            {
                                currentTable = row["TABLE_NAME"].ToString();
                                recordId = Convert.ToInt32(row["RECORD_ID"]);
                            }
                            if (!typeDic.ContainsKey(row["TABLE_NAME"].ToString()))
                            {
                                string classAssembly = row["CLASS_TYPE"].ToString()
                                                                        .Substring(0,
                                                                                   row["CLASS_TYPE"].ToString()
                                                                                                    .LastIndexOf('.'));
                                Assembly ass = Assembly.Load(classAssembly);
                                typeDic[row["TABLE_NAME"].ToString()] = ass.GetType(row["CLASS_TYPE"].ToString());
                            }
                        }
                        string connectionString = "Data Source=" + dbfilePath;
                        if (!string.IsNullOrEmpty(currentTable))
                        {
                            if (IsTbExist(currentTable, connectionString))
                            {
                                string sql = "select * from " + currentTable + " where Id>" + recordId;
                                if (SqliteCacheSetting.Instance.InsertType == 1)
                                    UpdateFirst(sql, connectionString, typeDic[currentTable], currentTable);
                                else
                                    SqlMerge(sql, connectionString, typeDic[currentTable], currentTable);
                            }
                        }
                        foreach (var tb in tblist)
                        {
                            if (IsTbExist(tb, connectionString))
                            {
                                string sql = "select * from " + tb;
                                if (SqliteCacheSetting.Instance.InsertType == 1)
                                    UpdateFirst(sql, connectionString, typeDic[tb], tb);
                                else
                                    SqlMerge(sql, connectionString, typeDic[tb], tb);
                            }
                        }
                        ProcessDbFile(dbfilePath);
                    }
                }
                DeleteRecord();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("处理历史数据时出现异常", ex);
            }
        }

        /// <summary>
        /// 判断sqlite文件中表是否存在
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        private bool IsTbExist(string tableName, string connectionString)
        {
            string checkTable = "select count(*) from sqlite_master where name='" + tableName + "' and type='table'";
            try
            {
                SqliteContext sqliteContext = new SqliteContext(new DBContextBridge
                {
                    ConnectionString = connectionString,
                    Provider = "sqlite",
                });
                int r = Convert.ToInt32(sqliteContext.ExecuteScalar(checkTable));
                if (r < 1)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("判断sqlite文件中表是否存在时出现异常", ex);
            }
        }

        /// <summary>
        /// 处理数据（无断点时）
        /// </summary>
        /// <param name="typeDic"></param>
        private void ProcessData(Dictionary<string, Type> typeDic)
        {
            try
            {
                string[] files = Directory.GetFiles(DbPath);
                List<string> readList = new List<string>();
                foreach (var file in files)
                {
                    if (file.Contains(".read."))
                    {
                        readList.Add(file);
                    }
                }
                foreach (var readFile in readList)
                {
                    //tblist = GetTables(readFile);
                    foreach (var tableName in typeDic.Keys)
                    {
                        InitRecordTable(readFile, typeDic[tableName].ToString(), tableName);
                    }
                    string connectionString = "Data Source=" + readFile;
                    foreach (var tb in typeDic.Keys)
                    {
                        if (IsTbExist(tb, connectionString))
                        {
                            string sql = "select * from " + tb;
                            if (SqliteCacheSetting.Instance.InsertType == 1)
                                UpdateFirst(sql, connectionString, typeDic[tb], tb);
                            else
                                SqlMerge(sql, connectionString, typeDic[tb], tb);
                        }
                    }
                    ProcessDbFile(readFile);
                    DeleteRecord();
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("处理历史数据时出现异常", ex);
            }
        }

        /// <summary>
        /// sqlite数据文件删除或移走
        /// </summary>
        /// <param name="filePath"></param>
        private void ProcessDbFile(string filePath)
        {
            if (SqliteCacheSetting.Instance.IsDbBak)
            {
                string[] fileDirs = filePath.Split(new[] { '\\' });
                string dirPath = DbBakPath + "\\" + DateTime.Now.ToString("yyyy-MM-dd");
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }
                File.Move(filePath, dirPath + "\\" + fileDirs[fileDirs.Length - 1] + ".bak");
            }
            else
            {
                File.Delete(filePath);
            }
        }

        private static bool _isFirst = true;

        /// <summary>
        /// 先更新后插入
        /// </summary>
        /// <param name="updateFirstSql"></param>
        /// <param name="connectionString"></param>
        /// <param name="type"></param>
        /// <param name="tableName"></param>
        private void UpdateFirst(string updateFirstSql, string connectionString, Type type, string tableName)
        {
            try
            {
                DataTable dtUpdate = GetSqliteDt(updateFirstSql, connectionString);
                if (dtUpdate != null)
                {
                    if (_isFirst)
                    {
                        Logger.WriteError("开始插入时间", DateTime.Now.ToString());
                        _isFirst = false;
                    }
                    //根据哪些列更新
                    Dictionary<string, DataType> keyDic = Class2DbHelper.Instance[type].GetKeyColumns();
                    //所有列
                    Dictionary<string, DataType> allColDic = Class2DbHelper.Instance[type].GetAllColumns();
                    //更新表达式
                    string setColumn = Class2DbHelper.Instance[type].GetUpdateExpr();
                    if (keyDic.Keys.Count > 0 && !string.IsNullOrEmpty(setColumn))
                    {
                        SingleCommit(dtUpdate, keyDic, allColDic, tableName, setColumn);
                    }
                    else
                    {
                        BatchCommit(dtUpdate, allColDic, tableName);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("执行先更新后插入方式时出现异常", ex);
            }
        }

        /// <summary>
        /// 单条更新插入
        /// </summary>
        /// <param name="dtUpdate"></param>
        /// <param name="keyDic"></param>
        /// <param name="allColDic"></param>
        /// <param name="tableName"></param>
        /// <param name="setColumn"></param>
        private void SingleCommit(DataTable dtUpdate, Dictionary<string, DataType> keyDic, Dictionary<string, DataType> allColDic, string tableName, string setColumn)
        {
            int count = 0;
            int transid = 0;
            int recordId = 0;
            int m = 0;
            try
            {
                foreach (DataRow row in dtUpdate.Rows)
                {
                    if (Convert.ToInt32(row["Id"]) > recordId)
                    {
                        recordId = Convert.ToInt32(row["Id"]);
                    }
                    if (transid == 0) transid = DB.DBManager.RegisteTrans();
                    int ret = 0;
                    string updateSql = GetUpdateSql(keyDic, tableName, setColumn, row);
                    ret = DB.DBManager.ExecuteSQLWithReturn(transid, updateSql);
                    count++;
                    if (ret == 0)
                    {
                        string insertSql = GetInsertSql(row, tableName, allColDic);
                        DB.DBManager.ExecuteSQL(transid, insertSql);
                    }
                    if (count == 10000 || count + 10000 * m == dtUpdate.Rows.Count)
                    {
                        DB.DBManager.TransCommit(transid);
                        m++;
                        count = 0;
                        transid = 0;
                        UpdateRecord(recordId, tableName);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("单条更新插入时出现异常", ex);
            }
            UpdateRecord(recordId, tableName);
        }

        /// <summary>
        /// 批量提交
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="allColDic"></param>
        /// <param name="tableName"></param>
        private void BatchCommit(DataTable dt, Dictionary<string, DataType> allColDic, string tableName)
        {
            try
            {
                int count = 0;
                int transid = 0;
                int recordId = 0;
                int m = 0;
                foreach (DataRow row in dt.Rows)
                {
                    if (Convert.ToInt32(row["Id"]) > recordId)
                    {
                        recordId = Convert.ToInt32(row["Id"]);
                    }
                    if (transid == 0) transid = DB.DBManager.RegisteTrans();
                    string insertSql = GetInsertSql(row, tableName, allColDic);
                    DB.DBManager.ExecuteSQL(transid, insertSql);
                    count++;
                    if (count == 10000 || count + 10000 * m == dt.Rows.Count)
                    {
                        DB.DBManager.TransCommit(transid);
                        m++;
                        count = 0;
                        transid = 0;
                        UpdateRecord(recordId, tableName);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("通过事务提交时出现异常", ex);
            }
        }

        /// <summary>
        /// Sql中Merge
        /// </summary>
        /// <param name="sqlMergeSql"></param>
        /// <param name="connectionString"></param>
        /// <param name="type"></param>
        /// <param name="tableName"></param>
        private void SqlMerge(string sqlMergeSql, string connectionString, Type type, string tableName)
        {
            try
            {
                DataTable dtMerge = GetSqliteDt(sqlMergeSql, connectionString);
                if (dtMerge != null)
                {
                    //所有列
                    Dictionary<string, DataType> allColDic = Class2DbHelper.Instance[type].GetAllColumns();
                    //更新表达式
                    string setColumn = Class2DbHelper.Instance[type].GetUpdateExpr();
                    int count = 0;
                    int transid = 0;
                    int m = 0;
                    int recordId = 0;
                    foreach (DataRow row in dtMerge.Rows)
                    {
                        if (Convert.ToInt32(row["Id"]) > recordId)
                        {
                            recordId = Convert.ToInt32(row["Id"]);
                        }
                        if (transid == 0) transid = DB.DBManager.RegisteTrans();
                        string mergeSql = GetMergeSql(row, tableName, setColumn, allColDic);
                        DB.DBManager.ExecuteSQL(transid, mergeSql);
                        count++;
                        if (count == 10000 || count + 10000 * m == dtMerge.Rows.Count)
                        {
                            DB.DBManager.TransCommit(transid);
                            UpdateRecord(recordId, tableName);
                            m++;
                            count = 0;
                            transid = 0;
                        }
                    }
                }
                //记录更新为-1
                UpdateRecord(-1, tableName);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("执行在Sql中Merge时出现异常", ex);
            }
        }

        /// <summary>
        /// Merge到Mysql的脚本
        /// </summary>
        /// <param name="row"></param>
        /// <param name="tableName"></param>
        /// <param name="setColumn"></param>
        /// <returns></returns>
        private string GetMergeSql(DataRow row, string tableName, string setColumn, Dictionary<string, DataType> allColDic)
        {
            StringBuilder sbInsert = new StringBuilder();
            sbInsert.Append("INSERT INTO ");
            sbInsert.Append(tableName);
            sbInsert.Append(" (");
            for (int i = 0; i < row.Table.Columns.Count; i++)
            {
                if (!"id".Equals(row.Table.Columns[i].ColumnName.ToLower()))
                {
                    sbInsert.Append(row.Table.Columns[i].ColumnName);
                    sbInsert.Append(",");
                }
            }
            sbInsert.Remove(sbInsert.Length - 1, 1);
            sbInsert.Append(" )VALUES( ");
            for (int i = 0; i < row.Table.Columns.Count; i++)
            {
                if (!"id".Equals(row.Table.Columns[i].ColumnName.ToLower()))
                {
                    sbInsert.Append(GetValueByType(allColDic[row.Table.Columns[i].ColumnName],
                                                   row[row.Table.Columns[i].ColumnName]));
                    sbInsert.Append(",");
                }
            }
            sbInsert.Remove(sbInsert.Length - 1, 1);
            sbInsert.Append(")");
            if (!string.IsNullOrEmpty(setColumn))
            {
                sbInsert.Append(" ON DUPLICATE KEY UPDATE " + setColumn);
            }
            return sbInsert.ToString();
        }

        /// <summary>
        /// 从Sqlite中获取数据
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        private DataTable GetSqliteDt(string sql, string connectionString)
        {
            SqliteDbUtility.Instance.Init(connectionString);
            DataSet ds = SqliteDbUtility.Instance.sqliteContext.GetDs("", sql);
            if (ds != null && ds.Tables != null && ds.Tables[0].Rows.Count > 0)
                return ds.Tables[0];
            return null;
        }

        /// <summary>
        /// 获取insert语句（先更新后插入的方式）
        /// </summary>
        /// <param name="row"></param>
        /// <param name="tableName"></param>
        /// <param name="allColDic"></param>
        /// <returns></returns>
        private string GetInsertSql(DataRow row, string tableName, Dictionary<string, DataType> allColDic)
        {
            StringBuilder sbInsert = new StringBuilder();
            sbInsert.Append("insert into ");
            sbInsert.Append(tableName);
            sbInsert.Append(" (");
            for (int i = 0; i < row.Table.Columns.Count; i++)
            {
                if (!"id".Equals(row.Table.Columns[i].ColumnName.ToLower()))
                {
                    sbInsert.Append(row.Table.Columns[i].ColumnName);
                    sbInsert.Append(",");
                }
            }
            sbInsert.Remove(sbInsert.Length - 1, 1);
            sbInsert.Append(" )VALUES( ");
            for (int i = 0; i < row.Table.Columns.Count; i++)
            {
                if (!"id".Equals(row.Table.Columns[i].ColumnName.ToLower()))
                {
                    sbInsert.Append(GetValueByType(allColDic[row.Table.Columns[i].ColumnName],
                                                   row[row.Table.Columns[i].ColumnName]));
                    sbInsert.Append(",");
                }
            }
            sbInsert.Remove(sbInsert.Length - 1, 1);
            sbInsert.Append(")");
            return sbInsert.ToString();
        }

        /// <summary>
        /// 获取update语句（先更新后插入的方式）
        /// </summary>
        /// <param name="keyDic"></param>
        /// <param name="tableName"></param>
        /// <param name="setColumn"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        private string GetUpdateSql(Dictionary<string, DataType> keyDic, string tableName, string setColumn, DataRow row)
        {
            StringBuilder sb = new StringBuilder();
            if (keyDic.Keys.Count > 0)
            {
                sb.Append("Update ");
                sb.Append(tableName);
                sb.Append(" set ");
                sb.Append(setColumn);
                sb.Append(" where ");
                int count = 0;
                foreach (var key in keyDic.Keys)
                {
                    if (count != 0)
                    {
                        sb.Append(" and ");
                    }
                    sb.Append(key);
                    sb.Append("=");
                    sb.Append(GetValueByType(keyDic[key], row[key]));
                    count++;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 根据类型获取值
        /// </summary>
        /// <param name="type"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        private string GetValueByType(DataType type, object val)
        {
            string fieldValue = string.Empty;
            if (val != null)
            {
                switch (type)
                {
                    case DataType.Date:
                        fieldValue = "'" + ((DateTime)val).ToString() + "'";
                        break;
                    case DataType.Interger:
                        if (val is bool)
                            fieldValue = ((bool)val) ? "1" : "0";
                        else
                            fieldValue = val.ToString();
                        break;
                    case DataType.Varchar2:
                        fieldValue = "'" + DB.ReplaceInvalidSQL(val.ToString()) + "'";
                        fieldValue = DB.DBManager.FormatEscapeChar(fieldValue);
                        break;
                    case DataType.Boolean:
                        fieldValue = ((bool)val) ? "1" : "0";
                        break;
                    case DataType.LongString:
                        fieldValue = DB.ReplaceInvalidSQL(val.ToString());
                        break;
                    case DataType.Enum:
                        fieldValue = ((int)val).ToString();
                        break;
                    case DataType.Double:
                        fieldValue = ((double)val).ToString();
                        break;
                    case DataType.Byte:
                        fieldValue = val.ToString();
                        break;
                    case DataType.Long:
                        fieldValue = ((long)val).ToString();
                        break;
                    case DataType.Decimal:
                        fieldValue = ((decimal)val).ToString();
                        break;
                    default:
                        fieldValue = "";
                        break;
                }
            }
            else
            {
                fieldValue = "NULL";
            }
            return fieldValue;
        }

        /// <summary>
        /// 初始化记录读取位置的表，RECORD_ID初始值为0
        /// </summary>
        private void InitRecordTable(string dbfileName, string classType, string tableName)
        {
            SqliteDbUtility.Instance.Init(string.Concat("Data Source=", RecordDbPath));
            string insertSql = "insert into " + RecordTableName + "(DBFILE_PATH,TABLE_NAME,CLASS_TYPE,RECORD_ID) VALUES('" + dbfileName + "', '" + tableName + "', '" + classType + "', " + 0 + ")";
            SqliteDbUtility.Instance.sqliteContext.ExecuteSQL("", insertSql);
        }

        /// <summary>
        /// 更新断点表的recordId
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="tableName"></param>
        private void UpdateRecord(int recordId, string tableName)
        {
            string connectionString = string.Concat("Data Source=", RecordDbPath);
            string updateSql = "update " + RecordTableName + " set record_id=" + recordId +
                               ",update_time=datetime(CURRENT_TIMESTAMP,'localtime') where TABLE_NAME='" + tableName + "'";
            SqliteDbUtility.Instance.Init(connectionString);
            SqliteDbUtility.Instance.sqliteContext.ExecuteSQL("", updateSql);
        }

        /// <summary>
        /// 清除断点记录
        /// </summary>
        private void DeleteRecord()
        {
            SqliteDbUtility.Instance.Init(string.Concat("Data Source=", RecordDbPath));
            string delSql = "delete from READ_RECORD ";
            SqliteDbUtility.Instance.sqliteContext.ExecuteSQL("", delSql);
        }

    }
}
