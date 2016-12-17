using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using GM.Orm.Db;

namespace GM.Utilities.SqliteCache
{
    public class Class2DbHelper
    {
        static Class2DbHelper cdh;
        private Hashtable releation = Hashtable.Synchronized(new Hashtable());

        public static Class2DbHelper Instance
        {
            get
            {
                if (cdh == null)
                    cdh = new Class2DbHelper();
                return cdh;
            }
        }

        private bool IsExistClassReleation(Type type)
        {
            if (releation.Contains(type))
                return true;
            return false;
        }

        private void AddClassReleation(Type type)
        {
            Class2TableRel cdr = new Class2TableRel();

            object[] objs;
            if (type.IsDefined(typeof(DbTableAttribute), true))
            {
                objs = type.GetCustomAttributes(typeof(DbTableAttribute), true);
                cdr.ClassAttribute = ((DbTableAttribute)objs[0]);
                cdr.ClassType = type;
            }
            else
            {
                throw new Exception(type.ToString() + "类未实现DbTableAttribute接口");
            }

            PropertyInfo[] propertyInfo = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (PropertyInfo p in propertyInfo)
            {
                if (p.IsDefined(typeof(DbFieldAttribute), true) && !p.IsDefined(typeof(DBFieldIgnoreAttribute), true))
                {
                    Property2FieldRel pfr = new Property2FieldRel();
                    object[] o = p.GetCustomAttributes(typeof(DbFieldAttribute), true);
                    pfr.PropertyInfo = p;
                    pfr.PropertyAttribute = ((DbFieldAttribute)o[0]);

                    if (pfr.PropertyAttribute.DataType == DataType.Null)
                        pfr.PropertyAttribute.DataType = GetDatabaseType(p.PropertyType);

                    cdr.AddField(pfr);
                }
            }

            releation.Add(type, cdr);
        }

        private DataType GetDatabaseType(Type type)
        {
            if (type == typeof(string))
                return DataType.Varchar2;
            else if (type == typeof(DateTime))
                return DataType.Date;
            else if (type.IsEnum)
                return DataType.Enum;
            else if (type == typeof(bool))
                return DataType.Boolean;
            else if (type == typeof(double))
                return DataType.Double;
            else if (type == typeof(byte))
                return DataType.Byte;
            else if (type == typeof(long))
                return DataType.Long;
            else if (type.IsPrimitive && type.IsValueType)
                return DataType.Interger;
            else if (type == typeof(decimal))
                return DataType.Decimal;
            else if ((type.GetCustomAttributes(typeof(CustomSerializableAttribute), false).Length > 0) || type.IsSerializable)
                return DataType.XmlObject;
            else
                throw new NotSupportedException("无法将指定的数据类型转换为合适的数据库类型");
        }

        public Class2TableRel this[Type type]
        {
            get
            {
                lock (releation.SyncRoot)
                {
                    if (!IsExistClassReleation(type))
                    {
                        AddClassReleation(type);
                    }
                    return (Class2TableRel)releation[type];
                }
            }
        }
    }

    //存储类的数据库定义
    public class Class2TableRel
    {
        private ArrayList ar = new ArrayList();
        public DbTableAttribute ClassAttribute;
        public Type ClassType;
        const string _defaultSequenceName = "DEFAULTAUTOSEQUENCE";

        public void AddField(Property2FieldRel pr)
        {
            ar.Add(pr);
        }

        public Property2FieldRel this[int index]
        {
            get
            {
                return (Property2FieldRel)ar[index];
            }
        }

        public Property2FieldRel this[string PropertyName]
        {
            get
            {
                for (int i = 0; i < ar.Count; i++)
                {
                    if (((Property2FieldRel)ar[i]).PropertyInfo.Name == PropertyName)
                    {
                        return (Property2FieldRel)ar[i];
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// 创建表脚本
        /// </summary>
        /// <returns></returns>
        public string GetCreateTableSql()
        {
            StringBuilder sbCreate = new StringBuilder();
            sbCreate.Append("CREATE TABLE ");
            sbCreate.Append(ClassAttribute.TableName);
            sbCreate.Append(" (Id integer PRIMARY KEY autoincrement, ");
            for (int i = 0; i < ar.Count; i++)
            {
                if (!"id".Equals(this[i].PropertyAttribute.AttributeName.ToLower()))
                {
                    var dbField = this[i].PropertyAttribute;
                    sbCreate.Append(dbField.AttributeName);
                    sbCreate.Append(" ");
                    if (DataType.Enum == dbField.DataType)
                    {   //枚举型转为Interger
                        sbCreate.Append("Interger");
                    }
                    else
                    {
                        sbCreate.Append(dbField.DataType);
                    }
                    if (dbField.Size > 0)
                    {
                        sbCreate.Append("(");
                        sbCreate.Append(dbField.Size);
                        sbCreate.Append(") ");
                    }
                    if (!string.IsNullOrEmpty(dbField.DefaultValue))
                    {
                        sbCreate.Append(" DEFAULT ");
                        sbCreate.Append(dbField.DefaultValue);
                    }
                    if (i < ar.Count - 2) { sbCreate.Append(", "); }
                    else
                    {
                        sbCreate.Append(")");
                    }
                }
            }
            return sbCreate.ToString();
        }

        public string GetInsertSqliteSql(SqliteEntity obj)
        {
            StringBuilder sbInsert = new StringBuilder();
            sbInsert.Append("INSERT INTO ");
            sbInsert.Append(ClassAttribute.TableName);
            sbInsert.Append(" (");
            for (int i = 0; i < ar.Count; i++)
            {
                if (!"id".Equals(this[i].PropertyAttribute.AttributeName.ToLower()) && string.IsNullOrEmpty(this[i].PropertyAttribute.DefaultValue))
                {
                    sbInsert.Append(this[i].PropertyAttribute.AttributeName);
                    if (i < ar.Count - 2) sbInsert.Append(", ");
                }
            }
            sbInsert.Append(" )VALUES( ");
            for (int i = 0; i < ar.Count; i++)
            {
                if (!"id".Equals(this[i].PropertyAttribute.AttributeName.ToLower()) && string.IsNullOrEmpty(this[i].PropertyAttribute.DefaultValue))
                {
                    sbInsert.Append(this[i].GetValue(obj));
                    if (i < ar.Count - 2) sbInsert.Append(",");
                }
            }
            sbInsert.Append(")");
            return sbInsert.ToString();
        }

        public string GetWhereConditionSql(SqliteEntity obj)
        {
            string strWhere = "";
            for (int i = 0; i < ar.Count; i++)
            {
                if (this[i].PropertyAttribute.IsKey || this[i].PropertyInfo.Name == "UpdateTime")
                {
                    if (strWhere != "") strWhere += " AND ";
                    strWhere += this[i].PropertyAttribute.AttributeName;
                    strWhere += " = ";
                    strWhere += this[i].GetValue(obj);
                }
            }
            return strWhere;
        }

        /// <summary>
        /// 获取主键列的名称和类型
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, DataType> GetKeyColumns()
        {
            Dictionary<string, DataType> keyDic = new Dictionary<string, DataType>();
            for (int i = 0; i < ar.Count; i++)
            {
                if (this[i].PropertyAttribute.IsKey)
                {
                    keyDic[this[i].PropertyAttribute.AttributeName] = this[i].PropertyAttribute.DataType;
                }
            }
            return keyDic;
        }

        /// <summary>
        /// 获取所有列的类型
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, DataType> GetAllColumns()
        {
            Dictionary<string, DataType> allColDic = new Dictionary<string, DataType>();
            for (int i = 0; i < ar.Count; i++)
            {
                if (!"id".Equals(this[i].PropertyAttribute.AttributeName.ToLower()))
                {
                    allColDic[this[i].PropertyAttribute.AttributeName] = this[i].PropertyAttribute.DataType;
                }
            }
            return allColDic;
        }

        /// <summary>
        /// 获取update脚本set部分
        /// </summary>
        /// <returns></returns>
        public string GetUpdateExpr()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ar.Count; i++)
            {
                if (!string.IsNullOrEmpty(this[i].PropertyAttribute.UpdateExpr))
                {
                    sb.Append(" ");
                    sb.Append(this[i].PropertyAttribute.UpdateExpr);
                    sb.Append(",");
                }
            }
            if (sb.ToString().Length > 0)
                return sb.ToString().Substring(0, sb.ToString().Length - 1);
            return string.Empty;
        }

    }
    //存储类的字段数据库定义
    public class Property2FieldRel
    {
        public PropertyInfo PropertyInfo;
        public DbFieldAttribute PropertyAttribute;

        public class XmlObjectConvertor
        {
            public static object GetObject(string xml)
            {
                if (string.IsNullOrEmpty(xml))
                    return null;

                int index = xml.IndexOf("-->\r\n");
                if (index == -1)
                    throw new ArgumentException("xml中不包含对象类型，无法反序列化");

                string typeString = xml.Substring(4, index - 4);
                Type type = Type.GetType(typeString, true);

                string xmlBody = xml.Substring(index + 5);

                return SerializeHelper.Deserialize(xmlBody, type);
            }

            public static string GetXml(object obj)
            {
                Type type = obj == null ? typeof(string) : obj.GetType();

                string typeString = type.AssemblyQualifiedName;
                string xml = SerializeHelper.Serialize(obj, type);

                return "<!--" + typeString + "-->\r\n" + xml;
            }
        }

        public string GetValue(SqliteEntity obj)
        {
            object val = this.PropertyInfo.GetValue(obj, null);
            string fieldValue = "";
            if (val != null)
            {
                switch (PropertyAttribute.DataType)
                {
                    case DataType.Date:
                        fieldValue = "'" + ((DateTime)val).ToString("s") + "'";
                        break;
                    case DataType.Interger:
                        if (val is bool)
                            fieldValue = ((bool)val) ? "1" : "0";
                        else
                            fieldValue = val.ToString();
                        break;
                    case DataType.Varchar2:
                        fieldValue = "'" + DB.ReplaceInvalidSQL(val.ToString()) + "'";
                        //fieldValue = DB.DBManager.FormatEscapeChar(fieldValue);
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
                    case DataType.XmlObject:
                        fieldValue = "'" + DB.ReplaceInvalidSQL(XmlObjectConvertor.GetXml(val)) + "'";
                        fieldValue = DB.DBManager.FormatEscapeChar(fieldValue);
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
    }
}
