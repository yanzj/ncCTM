USE [CTMDB]
GO


/*
/****** [sp_GetStockDailyClosePrices] ******/
*/
DROP PROCEDURE [dbo].[sp_GetStockDailyClosePrices]
GO
CREATE PROCEDURE [dbo].[sp_GetStockDailyClosePrices]
AS
BEGIN

	SET NOCOUNT ON

	DECLARE @loopDate datetime 
	DECLARE @nowDate date =GETDATE()

	SELECT @loopDate = ISNULL(MAX([TradeDate]), '2015-12-31') FROM [dbo].[TKLineToday]
	
	WHILE(@loopDate < @nowDate)
		BEGIN
			SET @loopDate =DATEADD(DAY,1,@loopDate)
			/* PRINT @loopDate */

			INSERT [dbo].[TKLineToday]
			SELECT [StockCode] 
						,[TradeDate]= CONVERT(datetime,@loopDate,120) 
						,[Close] 
			FROM
					(
					 SELECT [StockCode]
								 ,[TradeDate] 
								 ,[Close]  
								 ,ROW_NUMBER() OVER(PARTITION BY StockCode ORDER BY TradeDate DESC) RowNumber 
					 FROM [FinancialCenter].[dbo].[TKLine_Today] 
					 WHERE   [TradeDate] < DATEADD(DAY,1,@loopDate)
					)  AS t
			WHERE t.RowNumber =1					
		END

	IF(@loopDate = @nowDate)
		BEGIN
			DELETE FROM [dbo].[TKLineToday] WHERE [TradeDate] = @nowDate
			/* PRINT N'The Current Date Close Price Has Been Deleted !!!' */
			
			INSERT [dbo].[TKLineToday]
			SELECT [StockCode] 
						,[TradeDate]= CONVERT(datetime,@loopDate,120) 
						,[Close] 
			FROM
					(
					 SELECT [StockCode]
								 ,[TradeDate] 
								 ,[Close]  
								 ,ROW_NUMBER() OVER(PARTITION BY StockCode ORDER BY TradeDate DESC) RowNumber 
					 FROM [FinancialCenter].[dbo].[TKLine_Today] 
					 WHERE [TradeDate] < DATEADD(DAY,1,@loopDate)
					)  AS t
			WHERE t.RowNumber =1
			/* PRINT N'The Current Date Close Price Has Been Inserted !!!' */
		END

END
GO


/*
/****** [sp_GetAccountDetail] ******/
*/
DROP PROCEDURE [dbo].[sp_GetAccountDetail]
GO
CREATE PROCEDURE  [dbo].[sp_GetAccountDetail]
(
@IndustyId int = 0,
@AccountIds varchar(1000) = null,
@SecurityCode int = 0,
@AttributeCode int = 0,
@PlanCode int = 0,
@TypeCode int = 0,
@OnlyNeedAccounting bit = 0,
@ShowDisabled bit = 0
)
AS
BEGIN

	SET NOCOUNT ON

	DECLARE @commandText varchar(8000)

	SET @commandText = N'SELECT IndustryName =II.Name, OperatorNames =[dbo].[f_GetAccountOperatorNames](AI.Id), AI.*, DisplayMember = AI.Name + '' - '' + AI.SecurityCompanyName + '' - '' +AI.AttributeName + '' - '' +AI.TypeName
											 FROM [dbo].[AccountInfo]  AS AI 
											 LEFT JOIN [dbo].[IndustryInfo] AS II ON II.Id = AI.IndustryId
											 WHERE '

	IF( @IndustyId > 0 )
		SET @commandText +=N'#AND AI.IndustryId = ' + CAST(@IndustyId AS varchar(8))
	IF(LEN(@AccountIds)>0)
		SET @commandText +=N'#AND AI.Id IN(' + @AccountIds +')'
	IF( @SecurityCode > 0 )
		SET @commandText +=N'#AND AI.SecurityCompanyCode = ' +  CAST(@SecurityCode AS varchar(8))  
	IF( @AttributeCode > 0 )
		SET @commandText +=N'#AND AI.AttributeCode = ' +  CAST(@AttributeCode AS varchar(8))  
	IF( @PlanCode > 0 )
		SET @commandText +=N'#AND AI.PlanCode = ' +  CAST(@PlanCode AS varchar(8))  
	IF( @TypeCode > 0 )
		SET @commandText +=N'#AND AI.TypeCode = ' +  CAST(@TypeCode AS varchar(8))  
	IF( @OnlyNeedAccounting = 1 )
		SET @commandText +=N'#AND AI.NeedAccounting = 1'  
	IF( @ShowDisabled = 0 )
		SET @commandText +=N'#AND AI.IsDisabled = 0' 

	-- PRINT N'Before CommandText: ' + @commandText		

	DECLARE @sharpIndex int 
	SET @sharpIndex = CHARINDEX('#',@commandText)

	IF(@sharpIndex > 0)
		BEGIN
			SET @commandText=SUBSTRING(@commandText,1,@sharpIndex - 1) + SUBSTRING(@commandText,@sharpIndex + 4,LEN(@commandText))
			SET @commandText =REPLACE(@commandText,'#',' ')
		END
	ELSE
		BEGIN
			SET @commandText=REPLACE(@commandText,'WHERE',' ')			
		END	
		
	 -- PRINT N'After CommandText: ' + @commandText	

	EXEC( @commandText )
	
END
GO


/*
/****** [sp_GetDiffBetweenDeliveryAndDailyData] ******/
*/
DROP PROCEDURE [dbo].[sp_GetDiffBetweenDeliveryAndDailyData]
GO
CREATE PROCEDURE [dbo].[sp_GetDiffBetweenDeliveryAndDailyData]
(
@AccountId int,
@DateFrom datetime,
@DateTo datetime
)
AS
BEGIN	

	SET NOCOUNT ON

	SELECT	
			Delivery.* 
			,(ABS(ISNULL(Delivery.DE_TotalActualAmount,0)) - ABS(ISNULL(Daily.DA_TotalActualAmount,0)))AmountDiff
			,(ABS(ISNULL(Delivery.DE_TotalDealVolume,0)) - ABS(ISNULL(Daily.DA_TotalDealVolume,0))) VolumeDiff
			,Daily.*
	FROM
	(
		SELECT 
			TradeDate DE_TradeDate,
			StockCode DE_StockCode,
			MAX(StockName) DE_StockName, 
			DealFlag DE_DealFlag,
			CASE DealFlag 
				WHEN 1 THEN '买入'
				WHEN 0 THEN '卖出'
			END DE_DealName,
			SUM(ActualAmount) DE_TotalActualAmount,
			SUM(DealVolume) DE_TotalDealVolume
		FROM DeliveryRecord 
		WHERE AccountId = @AccountId AND TradeDate >= @DateFrom  AND TradeDate <= @DateTo 
		GROUP BY TradeDate,StockCode,DealFlag 
	) Delivery
	FULL JOIN 
	(
		SELECT 
			TradeDate DA_TradeDate,
			StockCode DA_StockCode,
			MAX(StockName) DA_StockName, 
			DealFlag DA_DealFlag,
			CASE DealFlag 
				WHEN 1 THEN '买入'
				WHEN 0 THEN '卖出'
			END DA_DealName,
			SUM(ActualAmount) DA_TotalActualAmount,
			SUM(DealVolume) DA_TotalDealVolume
		FROM DailyRecord 
		WHERE AccountId = @AccountId AND TradeDate >= @DateFrom  AND TradeDate <= @DateTo 
		GROUP BY TradeDate,StockCode,DealFlag 
	) Daily 
	ON Daily.DA_TradeDate=Delivery.DE_TradeDate AND Daily.DA_StockCode=Delivery.DE_StockCode AND Daily.DA_DealFlag = Delivery.DE_DealFlag

END
GO


/*
/****** [sp_DeliveryAccountInvestIncomeDetail] ******/
*/
DROP PROCEDURE [dbo].[sp_DeliveryAccountInvestIncomeDetail]
GO
CREATE PROCEDURE [dbo].[sp_DeliveryAccountInvestIncomeDetail]
(
@DateFrom datetime,
@DateTo datetime
)
AS
BEGIN	

	SET NOCOUNT ON
	
	SELECT 	
		MAX(DR.AccountId) AccId,
		MAX(DR.StockCode) SCode, 
		MAX(DR.StockName) SName, 
		SUM(DR.ActualAmount) TotalActualAmount,
		SUM(DR.DealVolume) HoldingVolume,
		ISNULL(MAX(TT.[Close]),0) LatestPrice,
		(ABS(SUM(DR.DealVolume)) * ISNULL(MAX(TT.[Close]),0))PositionValue,
		(SUM(DR.ActualAmount) + ABS(SUM(DR.DealVolume)) * ISNULL(MAX(TT.[Close]),0))AccumulatedProfit	
	INTO #TableEnd
	FROM DeliveryRecord DR
	LEFT JOIN TKLineToday TT
	ON DR.StockCode = TT.StockCode AND TT.TradeDate = @DateTo
	WHERE DR.TradeDate <= @DateTo
	GROUP BY DR.AccountId, DR.StockCode

	SELECT 	
		MAX(DR.AccountId) AccId,
		MAX(DR.StockCode) SCode, 
		--SUM(DR.ActualAmount) TotalActualAmount,
		--SUM(DR.DealVolume) HoldingVolume,
		--ISNULL(MAX(TT.[Close]),0) LatestPrice,
		--(ABS(SUM(DR.DealVolume)) * ISNULL(MAX(TT.[Close]),0))PositionValue,
		(SUM(DR.ActualAmount) + ABS(SUM(DR.DealVolume)) * ISNULL(MAX(TT.[Close]),0))AccumulatedProfit
	INTO #TableStart
	FROM DeliveryRecord DR
	LEFT JOIN TKLineToday TT
	ON DR.StockCode = TT.StockCode AND TT.TradeDate = DATEADD(DAY,-1,@DateFrom)
	WHERE DR.TradeDate < @DateFrom
	GROUP BY DR.AccountId, DR.StockCode

	--SELECT COUNT(*)START_NUM FROM #TableStart
	--SELECT COUNT(*)END_NUM FROM #TableEnd 

	SELECT 
		(CONVERT(varchar(10),@DateFrom,111) + ' - ' +  CONVERT(varchar(10),@DateTo,111)) QueryPeriod,
		(AI.Name + ' - ' + AI.SecurityCompanyName + ' - ' + AI.AttributeName + ' - ' + AI.TypeName) AccountDetail,
		(E.SCode + ' - ' + E.SName) StockDetail,	
		CAST(E.HoldingVolume AS decimal(18,0)) HoldingVolume,
		E.LatestPrice,
		E.PositionValue,
		ISNULL((E.AccumulatedProfit - S.AccumulatedProfit),0) Profit,
		E.AccumulatedProfit,	
		E.SCode StockCode,
		E.SName StockName,	
		AI.Id AccountId,
		AI.Name AccountName,
		AI.SecurityCompanyName,
		AI.AttributeName 	
	FROM #TableStart S
	FULL JOIN #TableEnd E
	ON S.AccId = E.AccId AND S.SCode = E.SCode
	LEFT JOIN AccountInfo AI
	ON E.AccId = AI.Id
	ORDER BY AccountDetail, StockDetail

	DROP TABLE #TableStart
	DROP TABLE #TableEnd

END
GO


/*
/****** [sp_AccountInvestFundDetail] ******/
*/
DROP PROCEDURE [dbo].[sp_AccountInvestFundDetail]
GO
CREATE PROCEDURE [dbo].[sp_AccountInvestFundDetail]
(
@DateFrom datetime,
@DateTo datetime
)	
AS
BEGIN	

	SET NOCOUNT ON
	
	/****** 账户最新月结信息 ******/
	SELECT 
		MAX(AccountId) AccountId,
		MAX(Amount) Amount,
		MAX([Month]) SettleMonth,
		(CAST(MAX([Month]) AS varchar) + '01') FirstDayOfSettleMonth
	INTO #Settle
	FROM AccountInitialFund 
	GROUP BY AccountId


	/****** 开始查询日期的账户资金信息 ******/
	SELECT 		
		MAX(S.AccountId) AccountId, 		
		(ISNULL(MAX(S.Amount),0) + ISNULL(SUM(T.TransferAmount),0)) InitialAmount	
	INTO #QueryInitial
	FROM #Settle S 	
	LEFT JOIN AccountFundTransfer T
	ON T.AccountId = S.AccountId AND T.TransferDate BETWEEN S.FirstDayOfSettleMonth AND @DateFrom
	GROUP BY S.AccountId		
	

	/****** 查询时间段的账户资金调拨信息 ******/
	SELECT 		
		AccountId, 
		FlowFlag,
		ISNULL(SUM(TransferAmount),0) TransferAmount 
	INTO #Transfer
	FROM AccountFundTransfer 
	WHERE TransferDate BETWEEN @DateFrom AND @DateTo
	GROUP BY AccountId, FlowFlag
	

	/****** 账户投资资金明细查询结果 ******/
	SELECT 
		AI.Id AccountId, 
		AI.Code AccountCode,
		AI.Name AccountName, 
		AI.SecurityCompanyName, 
		AI.AttributeName,
		ISNULL(Q.InitialAmount,0) InitialAmount,
		ISNULL(T1.TransferAmount,0) InAmount, 
		ISNULL(T0.TransferAmount,0) OutAmount, 
		(ISNULL(Q.InitialAmount,0) + ISNULL(T1.TransferAmount,0) + ISNULL(T0.TransferAmount,0)) FinalAmount
	FROM  #QueryInitial Q 
	LEFT JOIN  AccountInfo AI
	ON AI.Id = Q.AccountId 
	LEFT JOIN #Transfer T1
	ON T1.AccountId =Q.AccountId  AND T1.FlowFlag =1
	LEFT JOIN #Transfer T0
	ON T0.AccountId =Q.AccountId  AND T0.FlowFlag = 0
	ORDER BY AccountName, SecurityCompanyName, AttributeName 
	
	/****** Drop Temp Table ******/
	DROP TABLE #Settle 
	DROP TABLE #QueryInitial 
	DROP TABLE #Transfer 
	
  
END
GO


/*
/****** [sp_AccountFundSettleProcess] ******/
*/
DROP PROCEDURE [dbo].[sp_AccountFundSettleProcess]
GO
CREATE PROCEDURE [dbo].[sp_AccountFundSettleProcess]
AS
BEGIN

	SET NOCOUNT ON

	DECLARE @currentSettleMonth int = 0

	SELECT @currentSettleMonth = ISNULL(MAX([Month]),0)	FROM AccountInitialFund WHERE IsInitial = 0

	IF @currentSettleMonth = 0 
	BEGIN
		SELECT @currentSettleMonth = ISNULL(MIN([Month]),0)	FROM AccountInitialFund WHERE IsInitial = 1
	END
	
	IF @currentSettleMonth = 0 	RETURN

	DECLARE @firstDayOfSettleMonth varchar(10) = CAST(@currentSettleMonth AS varchar) + '01'
	DECLARE @lastDayOfSettleMonth varchar(10) = [dbo].[f_GetLastDayOfMonth](@firstDayOfSettleMonth)

	DECLARE @initialMonth int = YEAR(DATEADD(M,1,@firstDayOfSettleMonth))*100 + MONTH(DATEADD(M,1,@firstDayOfSettleMonth))

	INSERT INTO AccountInitialFund (AccountId,AccountCode,[Month],Amount,IsInitial)
	SELECT 
		AIF.AccountId,
		AIF.AccountCode,
		@initialMonth,
		AIF.Amount + ISNULL(T.TransferAmount,0),
		0
	FROM AccountInitialFund AIF
	LEFT JOIN
	(
		SELECT 
			MAX(AccountId) AccountId,
			SUM(TransferAmount) TransferAmount
		FROM AccountFundTransfer
		WHERE TransferDate BETWEEN @firstDayOfSettleMonth AND @lastDayOfSettleMonth
	) T
	ON AIF.AccountId = t.AccountId 
	WHERE AIF.[Month] = @currentSettleMonth
 
END
GO


/*
/****** [sp_AccountFundRevokeProcess] ******/
*/
DROP PROCEDURE [dbo].[sp_AccountFundRevokeProcess]
GO
CREATE PROCEDURE [dbo].[sp_AccountFundRevokeProcess]
AS
BEGIN

	SET NOCOUNT ON

	DECLARE @currentSettleMonth int = 0

	SELECT @currentSettleMonth = ISNULL(MAX([Month]),0)	FROM AccountInitialFund WHERE IsInitial = 0

	IF @currentSettleMonth = 0 	RETURN

	DELETE 
	FROM AccountInitialFund 
	WHERE IsInitial = 0 AND [Month] = @currentSettleMonth
	
 
END
GO


/*
/****** [sp_GetInvestmentDecisionForm] ******/
*/
DROP PROCEDURE [dbo].[sp_GetInvestmentDecisionForm]
GO
CREATE PROCEDURE [dbo].[sp_GetInvestmentDecisionForm]
AS
BEGIN

	SET NOCOUNT ON

	SELECT 	
		CASE IDF.[Status]
			WHEN 1 THEN '已提交'
			WHEN 2 THEN '进行中'
			WHEN 3 THEN '申请通过'
			WHEN 4 THEN '申请不通过'
		END StatusName,
		CASE IDF.TradeType
			WHEN 1 THEN '目标'
			WHEN 2 THEN '波段'
		END TradeTypeName,
		CASE IDF.DealFlag 
			WHEN 1 THEN '买入'
			WHEN 0 THEN '卖出'
		END DealFlagName,
		U.Name ApplyUserName,
		T.TotalWeight * 100 VotePoint,
		IDF.*
	FROM InvestmentDecisionForm	IDF
	LEFT JOIN UserInfo U
	ON IDF.ApplyUser = U.Code
	LEFT JOIN 
	(
		SELECT 
			FormSerialNo,
			SUM([Weight]) TotalWeight
		FROM InvestmentDecisionVote 	
		WHERE Flag = 1 AND [Type] =2
		GROUP BY FormSerialNo
	) T
	ON IDF.SerialNo = T.FormSerialNo

 
END
GO


/*
/****** [sp_InvestmentDecisionVoteProcess] ******/
*/
DROP PROCEDURE [dbo].[sp_InvestmentDecisionVoteProcess]
GO
CREATE PROCEDURE [dbo].[sp_InvestmentDecisionVoteProcess]
(
@InvestorCode varchar(50),
@FormSerialNo varchar(50),
@VoteFlag int,
@Reason varchar(1000)
)
AS
BEGIN

	SET NOCOUNT ON

	DECLARE @voteNumber int = 0

	SELECT @voteNumber = COUNT(UserCode) FROM InvestmentDecisionVote WHERE UserCode = @InvestorCode AND FormSerialNo = @FormSerialNo

	IF(@voteNumber = 0)
		BEGIN
			INSERT INTO InvestmentDecisionVote (AuthorityLevel,Flag,FormSerialNo,Reason,[Type], UserCode,VoteTime,[Weight])
			VALUES(0,@VoteFlag,@FormSerialNo,@Reason,[dbo].[f_GetIDVoteType](@FormSerialNo,@InvestorCode),@InvestorCode,GETDATE(),0)
		END
	ELSE
		BEGIN
			UPDATE InvestmentDecisionVote 
			SET Flag = @VoteFlag, Reason = @Reason, VoteTime = GETDATE()
			WHERE UserCode = @InvestorCode AND FormSerialNo = @FormSerialNo
		END

	UPDATE InvestmentDecisionForm 
	SET [Status] = [dbo].[f_GetIDFStatus](@FormSerialNo), UpdateTime = GETDATE()
	WHERE SerialNo = @FormSerialNo 
 
END
GO


/*
/****** [sp_GetIDVoteResult] ******/
*/
DROP PROCEDURE [dbo].[sp_GetIDVoteResult]
GO
CREATE PROCEDURE [dbo].[sp_GetIDVoteResult]
(
@FormSerialNo varchar(50)
)
AS
BEGIN

	SET NOCOUNT ON

	SELECT 
		--ROW_NUMBER() OVER( ORDER BY V.UserCode) 编号,
		U.Code 人员编号,
		U.Name 姓名,
		CAST(V.[Weight]*100 AS varchar ) + '%' 权重,
		CASE V.Flag
			WHEN 0 THEN '未投票'
			WHEN 1 THEN '赞同'
			WHEN 2 THEN '反对'
			WHEN 3 THEN '弃权'
		END	投票信息,
		V.Reason 理由,
		V.VoteTime 投票日期,
		''  确定日期
	FROM InvestmentDecisionVote V
	LEFT JOIN UserInfo U
	ON V.UserCode = U.Code	
	WHERE V.FormSerialNo =@FormSerialNo AND((V.Flag != 0) OR (V.Flag = 0 AND V.[Type] != 3))
	ORDER BY V.[Weight] DESC, V.UserCode
 
END
GO