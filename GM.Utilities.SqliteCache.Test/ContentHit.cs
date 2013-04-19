using System;
using System.Collections.Generic;
using System.Text;

namespace GM.Utilities.SqliteCache.Test
{
    [DbTable("CONTENT_HIT")]
    public class ContentHit : SqliteEntity
    {
        [DbField("CONTENT_OPID", Size = 20)]
        public string ContentOpid { get; set; }
        [DbField("CONTENT_TYPE", Size = 2)]
        public int ContentType { get; set; }
        [DbField("PHYSICAL_ENTITY_ID", Size = 9)]
        public int PhysicalEntityId { get; set; }
        [DbField("FILE_TYPE", Size = 9)]
        public int FileType { get; set; }
        [DbField("DOMAIN_ID", Size = 9)]//, IsKey = true
        public int DomainId { get; set; }
        [DbField("OUTER_ID", Size = 1024)]
        public string OuterId { get; set; }
        [DbField("IS_HIT", Size = 1)]//, IsKey = true
        public int IsHit { get; set; }
        [DbField("VISIT_COUNT", Size = 9, UpdateExpr = "VISIT_COUNT=VISIT_COUNT+1")]
        public int VisitCount { get; set; }
        [DbField("UPDATE_TIME", DefaultValue = "(datetime('now', 'localtime'))")]
        public DateTime UpdateTime { get; set; }
        [DbField("CREATE_TIME")]//, IsKey = true
        public DateTime CreateTime { get; set; }
    }
}
