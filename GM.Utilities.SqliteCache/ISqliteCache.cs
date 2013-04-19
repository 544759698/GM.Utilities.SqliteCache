using System;
using System.Collections.Generic;
using System.Text;

namespace GM.Utilities.SqliteCache
{
    public interface ISqliteCache
    {
        void Add(SqliteEntity entity);
        void Init(int queueCount);
    }
}
