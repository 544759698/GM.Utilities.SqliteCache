using System;
using System.Collections.Generic;
using System.Text;

namespace GM.Utilities.SqliteCache
{
    /// <summary>
    /// 标记数据库一个表的类
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DbTableAttribute : Attribute
    {
        public DbTableAttribute(string tableName)
        {
            TableName = tableName;
        }

        public string TableName { get; set; }
    }

    /// <summary>
    /// 标记数据库字段的描述，类型，和关键字(关键字生成由递加生成),
    /// 多个关键字对象由输入的id决定insert
    /// update的时候由关键字做过滤条件
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class DbFieldAttribute : Attribute
    {
        public DbFieldAttribute(string attributeName)
        {
            AttributeName = attributeName;
            DataType = DataType.Null;
            Size = 0;
            DefaultValue = string.Empty;
        }

        public DbFieldAttribute(string attributeName, DataType dataType)
        {
            AttributeName = attributeName;
            DataType = dataType;
            Size = 0;
            DefaultValue = string.Empty;
        }

        public DbFieldAttribute(string attributeName, DataType dataType, int size)
        {
            AttributeName = attributeName;
            DataType = dataType;
            Size = size;
            DefaultValue = string.Empty;
        }

        public DbFieldAttribute(string attributeName, DataType dataType, string defaultValue)
        {
            AttributeName = attributeName;
            DataType = dataType;
            Size = 0;
            DefaultValue = defaultValue;
        }

        public DbFieldAttribute(string attributeName, DataType dataType, int size, string defaultValue)
        {
            AttributeName = attributeName;
            DataType = dataType;
            Size = size;
            DefaultValue = defaultValue;
        }

        public DbFieldAttribute(string attributeName, DataType dataType, bool isKey)
        {
            AttributeName = attributeName;
            DataType = dataType;
            IsKey = isKey;
        }

        public DbFieldAttribute(string attributeName, DataType dataType, bool isKey, bool isAutoIncrease)
        {
            AttributeName = attributeName;
            DataType = dataType;
            IsKey = isKey;
        }

        /// <summary>
        /// 字段数据类型
        /// </summary>
        public DataType DataType;
        /// <summary>
        /// 字段名
        /// </summary>
        public string AttributeName;
        /// <summary>
        /// 字段长度
        /// </summary>
        public int Size;
        /// <summary>
        /// 默认值
        /// </summary>
        public string DefaultValue;
        /// <summary>
        /// 更新表达式
        /// </summary>
        public string UpdateExpr;
        /// <summary>
        /// 是否为where条件中的列
        /// </summary>
        public bool IsKey;

    }
    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DBFieldIgnoreAttribute : Attribute
    {
        public DBFieldIgnoreAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DBAutoSequenceAttribute : DbFieldAttribute
    {
        public string AutoSequeneceName;

        public DBAutoSequenceAttribute(string attributename)
            : base(attributename)
        {
            DataType = DataType.Interger;
            IsKey = true;
        }

        public DBAutoSequenceAttribute(string attributename, string autosequenece)
            : base(attributename)
        {
            DataType = DataType.Interger;
            AutoSequeneceName = autosequenece;
            IsKey = true;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SqliteSequenceAttribute : DbFieldAttribute
    {
        public SqliteSequenceAttribute(string attributename)
            : base(attributename)
        {
            DataType = DataType.Interger;
            IsKey = true;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SqliteDateTimeAttribute : DbFieldAttribute
    {
        public SqliteDateTimeAttribute(string attributename)
            : base(attributename)
        {
            DataType = DataType.Date;
        }
    }

    /// <summary>
    /// 数据库字段的类型
    /// </summary>
    public enum DataType
    {
        Null,
        Varchar2,
        Number,
        Interger,
        Date,
        Boolean,
        LongString,
        Enum,
        XmlObject,
        Double,
        Byte,
        Long,
        Decimal
    }

    /// <summary>
    /// 用于标识可序列化的接口对象
    /// </summary>
    public class CustomSerializableAttribute : Attribute
    {
    }
}
