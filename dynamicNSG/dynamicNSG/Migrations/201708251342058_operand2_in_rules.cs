namespace dynamicNSG.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class operand2_in_rules : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Rules", "Operand1", c => c.String());
            AddColumn("dbo.Rules", "Operand2", c => c.String());
            DropColumn("dbo.Rules", "Operand");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Rules", "Operand", c => c.String());
            DropColumn("dbo.Rules", "Operand2");
            DropColumn("dbo.Rules", "Operand1");
        }
    }
}
