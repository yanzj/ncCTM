USE [CTMDB]
GO


/*
/****** [f_GetAccountOperatorNames] ******/
*/
DROP FUNCTION [dbo].[f_GetAccountOperatorNames]
GO
CREATE FUNCTION [dbo].[f_GetAccountOperatorNames](@AccountId int)
RETURNS varchar(8000)
AS
BEGIN

    DECLARE @names varchar(8000)

    SET @names = ''

    SELECT @names = @names + ';' + U.Name
    FROM [dbo].UserInfo AS U
    WHERE U.Id IN (SELECT AO.OperatorId  FROM AccountOperator AS AO WHERE AO.AccountId = @AccountId)

    RETURN STUFF(@names, 1, 1, '')
		
END
GO


/*
/****** [f_GetFirstDayOfMonth] ******/
*/
DROP FUNCTION [dbo].[f_GetFirstDayOfMonth]
GO
CREATE FUNCTION [dbo].[f_GetFirstDayOfMonth](@CurrentDate datetime)
RETURNS datetime
AS
BEGIN    

    RETURN DATEADD(m,DATEDIFF(m,0,@CurrentDate),0)	
		
END
GO


/*
/****** [f_GetLastDayOfMonth] ******/
*/
DROP FUNCTION [dbo].[f_GetLastDayOfMonth]
GO
CREATE FUNCTION [dbo].[f_GetLastDayOfMonth](@CurrentDate datetime)
RETURNS datetime
AS
BEGIN    

    RETURN DATEADD(d,-1,DATEADD(m,DATEDIFF(m,0,@CurrentDate)+1,0))	
		
END
GO


/*
/****** [f_GetIDFStatus] ******/
*/
DROP FUNCTION [dbo].[f_GetIDFStatus]
GO
CREATE FUNCTION [dbo].[f_GetIDFStatus](@SerialNo varchar(50))
RETURNS int
AS
BEGIN    

	-- 申请单Status：1-已提交 2-进行中 3-申请通过 4-申请不通过 --
	-- 投票Flag：0-未投票 1-赞同 2-反对 3-弃权 --
	-- 投票Type：1-申请人 2-决策委员会 3-普通交易员 99-一票否决 -- 

	DECLARE @status int = 0

	DECLARE @voteNumber int = 0	
	SELECT @voteNumber = COUNT(UserCode)	FROM InvestmentDecisionVote 	WHERE FormSerialNo = @SerialNo  AND [Type] != 1 AND Flag > 0

	IF(@voteNumber = 0) 
		SET @status = 1
	ELSE
		BEGIN

			DECLARE @oneVoteVetoFlag int = 0
			SELECT @oneVoteVetoFlag = Flag FROM InvestmentDecisionVote WHERE FormSerialNo = @SerialNo  AND [Type] = 99

			IF (@oneVoteVetoFlag = 1)
				SET @status =  3 		
			ELSE IF (@oneVoteVetoFlag = 2)
				SET @status = 4
			ELSE
				BEGIN
					DECLARE @notVotecommittee int = 0
					SELECT @notVotecommittee = COUNT(UserCode) FROM InvestmentDecisionVote 	WHERE FormSerialNo = @SerialNo  AND [Type] = 2 AND Flag = 0
				
					IF(@notVotecommittee > 0) 
						SET @status =  2 		
					ELSE
						BEGIN
							DECLARE @point int = 0
							SELECT @point = SUM([Weight]) *100	FROM InvestmentDecisionVote 	WHERE FormSerialNo = @SerialNo AND Flag = 1	
							IF(@point > 60) 
								SET @status = 3
							ELSE			
								SET @status = 4
						END			
				END
				

			
		END

	RETURN @status
		
END
GO