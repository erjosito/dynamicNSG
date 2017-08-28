namespace dynamicNSG.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class policy_int : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Policies",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Action = c.String(),
                        Src = c.String(),
                        Dst = c.String(),
                        Prot = c.String(),
                        Range = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Policies");
        }
    }
}
