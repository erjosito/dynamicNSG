namespace dynamicNSG.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class tags3 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Tags",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        VmId = c.String(),
                        TagName = c.String(),
                        TagValue = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Tags");
        }
    }
}
