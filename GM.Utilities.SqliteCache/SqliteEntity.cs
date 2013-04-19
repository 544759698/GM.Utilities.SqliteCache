using System;
using System.Collections.Generic;
using System.Text;

namespace GM.Utilities.SqliteCache
{
    public abstract class SqliteEntity
    {
        [DbField("Id")]
        public int Id { get; private set; }
        //private EnumInsertType _typeValue = EnumInsertType.UpdateFirst;
        //[DbField("INSERT_TYPE")]
        //public EnumInsertType InsertType
        //{
        //    get { return _typeValue; }
        //    set { _typeValue = value; }
        //}

        public abstract object GetValue(object instance, string memberName);
    }
}
