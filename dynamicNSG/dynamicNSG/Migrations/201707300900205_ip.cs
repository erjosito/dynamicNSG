namespace dynamicNSG.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ip : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.IPs",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        NicId = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.NICs",
                c => new
                    {
                        NicId = c.String(nullable: false, maxLength: 128),
                        VmId = c.String(),
                    })
                .PrimaryKey(t => t.NicId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.NICs");
            DropTable("dbo.IPs");
        }
    }
}
