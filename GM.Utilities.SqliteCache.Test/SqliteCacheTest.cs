using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace GM.Utilities.SqliteCache.Test
{
    [TestFixture]
    class SqliteCacheTest
    {
        [Test]
        public void UpdateFirstTest()
        {
            try
            {
                CreateData(100000, 2);
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void CreateData( int totalCount, int writeThdCount)
        {
            SqliteCache cache = SqliteCache.Instance;
            //cache.Init(writeThdCount);
            DateTime dtNow = DateTime.Now;
            Random ran = new Random();
            for (int i = 0; i < totalCount; i++)
            {
                if (i % 10 == 0)
                {
                    dtNow = dtNow.AddSeconds(1);
                }
                ContentHit entity = new ContentHit()
                {
                    ContentOpid = "ss01",
                    ContentType = 3,
                    CreateTime = dtNow,
                    DomainId = ran.Next(1000, 1010),
                    FileType = 1,
                    IsHit = ran.Next(0, 2),
                    OuterId = "Qingniu/PhysicalFile00000000000000597258",
                    PhysicalEntityId = 1,
                    VisitCount = 1
                };
                cache.Add(entity);
            }
        }
    }
}
