namespace dynamicNSG.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class keineAhnungWarum : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.NSGrules",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        nsgName = c.String(),
                        direction = c.String(),
                        order = c.Int(nullable: false),
                        action = c.String(),
                        srcIp = c.String(),
                        srcProt = c.String(),
                        srcPort = c.String(),
                        dstIp = c.String(),
                        dstProt = c.String(),
                        dstPort = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.NSGrules");
        }
    }
}
