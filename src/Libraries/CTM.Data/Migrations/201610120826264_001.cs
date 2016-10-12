namespace CTM.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _001 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AccountFundTransfer",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AccountId = c.Int(nullable: false),
                        AccountCode = c.String(),
                        TransferDate = c.DateTime(nullable: false),
                        TransferType = c.Int(nullable: false),
                        TransferAmount = c.Decimal(nullable: false, precision: 24, scale: 4),
                        FlowFlag = c.Boolean(nullable: false),
                        TargetAccountId = c.Int(nullable: false),
                        TargetAccountCode = c.String(),
                        Balance = c.Decimal(nullable: false, precision: 24, scale: 4),
                        OperateTime = c.DateTime(nullable: false),
                        Operator = c.String(),
                        Remarks = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.AccountFundTransfer");
        }
    }
}
