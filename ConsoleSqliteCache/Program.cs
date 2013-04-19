using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using GM.Utilities.SqliteCache;

namespace ConsoleSqliteCache
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff"));
                CreateData(100000, 1);
                //ContentHit content = new ContentHit();
                //PropertyInfo[] pis = content.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            Console.ReadKey();
        }

        public static void UpdateFirstTest()
        {
            try
            {
                CreateData(100000, 1);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            Console.ReadKey();
        }

        private static void CreateData(int totalCount, int writeThdCount)
        {
            SqliteCache cache = SqliteCache.Instance;
            cache.Init(writeThdCount);
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
