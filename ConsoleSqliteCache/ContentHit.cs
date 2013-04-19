using System;
using System.Collections.Generic;
using System.Text;
using GM.Utilities.SqliteCache;

namespace ConsoleSqliteCache
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
        [DbField("VISIT_COUNT", Size = 9, UpdateExpr = "VISIT_COUNT=VISIT_COUNT+1")]//, UpdateExpr = "VISIT_COUNT=VISIT_COUNT+1"
        public int VisitCount { get; set; }
        [DbField("UPDATE_TIME", DefaultValue = "(datetime('now', 'localtime'))")]//sqlite的脚本语法
        public DateTime UpdateTime { get; set; }
        [DbField("CREATE_TIME")]//, IsKey = true
        public DateTime CreateTime { get; set; }

        public override object GetValue(object instance, string memberName)
        {
            var content = instance as ContentHit;
            if (content != null)
            {
                switch (memberName)
                {
                    case "ContentOpid": return content.ContentOpid;
                    case "ContentType": return content.ContentType;
                    case "PhysicalEntityId": return content.PhysicalEntityId;
                    case "FileType": return content.FileType;
                    case "DomainId": return content.DomainId;
                    case "OuterId": return content.OuterId;
                    case "IsHit": return content.IsHit;
                    case "VisitCount": return content.VisitCount;
                    case "UpdateTime": return content.UpdateTime;
                    case "CreateTime": return content.CreateTime;
                    default:
                        return null;
                }
            }
            else
                throw new InvalidProgramException();
        }
    }
}
