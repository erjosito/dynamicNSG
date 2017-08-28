namespace dynamicNSG.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class policy_sequence : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Policies", "Order", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Policies", "Order");
        }
    }
}
