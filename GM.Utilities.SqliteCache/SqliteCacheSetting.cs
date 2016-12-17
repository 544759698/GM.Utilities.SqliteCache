using System;
using System.Collections.Generic;
using System.Text;

namespace GM.Utilities.SqliteCache
{
    class SqliteCacheSetting
    {
        private static SqliteCacheSetting _instance;
        public static SqliteCacheSetting Instance
        {
            get { return _instance ?? (_instance = SettingManager.Instance.GetSetting<SqliteCacheSetting>()); }
        }

        /// <summary>
        /// 从sqlite读数据的时间间隔(单位：秒)
        /// </summary>
        [XmlSetting("ReadThreadInterval", "10")]
        public int ReadThreadInterval = 10;

        /// <summary>
        /// 向sqlite写数据的时间间隔(单位：秒)
        /// </summary>
        [XmlSetting("WriteThreadInterval", "5")]
        public int WriteThreadInterval = 5;

        /// <summary>
        /// 创建sqlite文件的间隔(单位：秒)
        /// </summary>
        [XmlSetting("SqliteFileInterval", "60")]
        public int SqliteFileInterval = 60;

        /// <summary>
        /// 是否备份Sqlite数据库文件
        /// </summary>
        [XmlSetting("IsDbBak", false)]
        public bool IsDbBak = false;

        /// <summary>
        /// 插入方式：1 先更新后插入 2 用sql merge （需要表有主键或唯一索引）
        /// </summary>
        [XmlSetting("InsertType", "1")]
        public int InsertType = 1;
    }
}
