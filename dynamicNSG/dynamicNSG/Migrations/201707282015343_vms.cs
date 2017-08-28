namespace dynamicNSG.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class vms : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.VMs",
                c => new
                    {
                        VmId = c.String(nullable: false, maxLength: 128),
                        Name = c.String(),
                        OS = c.String(),
                        ResourceGroup = c.String(),
                    })
                .PrimaryKey(t => t.VmId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.VMs");
        }
    }
}
