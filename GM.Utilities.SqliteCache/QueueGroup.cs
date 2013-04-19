using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace GM.Utilities.SqliteCache
{
    class QueueGroup
    {
        public Dictionary<Type, Queue> QueueDic
        {
            get;
            set;
        }
    }
}
