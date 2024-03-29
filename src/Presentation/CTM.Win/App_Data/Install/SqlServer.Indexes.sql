USE [CTMDB]
GO

DROP INDEX [IX_TKLineToday_StockCode_TradeDate] ON [dbo].[TKLineToday]
GO

CREATE NONCLUSTERED INDEX [IX_TKLineToday_StockCode_TradeDate] ON [dbo].[TKLineToday]
(
	[StockCode] ASC,
	[TradeDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO


