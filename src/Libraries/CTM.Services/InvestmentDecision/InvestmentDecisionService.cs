﻿using System;
using System.Collections.Generic;
using System.Linq;
using CTM.Core;
using CTM.Core.Data;
using CTM.Core.Domain.InvestmentDecision;
using CTM.Data;
using CTM.Services.Common;

namespace CTM.Services.InvestmentDecision
{
    public partial class InvestmentDecisionService : IInvestmentDecisionService
    {
        #region Fields

        private readonly IRepository<InvestmentDecisionCommittee> _IDCRepository;
        private readonly IRepository<InvestmentDecisionForm> _IDFRepository;
        private readonly IRepository<InvestmentDecisionVote> _IDVRepository;

        private readonly ICommonService _commonService;

        private readonly IDbContext _dbContext;

        #endregion Fields

        #region Constructors

        public InvestmentDecisionService(
        IRepository<InvestmentDecisionCommittee> IDCRepository,
        IRepository<InvestmentDecisionForm> IDFRepository,
        IRepository<InvestmentDecisionVote> IDVRepository,
        ICommonService commonService,
        IDbContext dbContext)
        {
            this._IDCRepository = IDCRepository;
            this._IDFRepository = IDFRepository;
            this._IDVRepository = IDVRepository;

            this._commonService = commonService;
            this._dbContext = dbContext;
        }

        #endregion Constructors

        #region Methods

        public virtual void SubmitInvestmentDecisionApplication(InvestmentDecisionForm entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _IDFRepository.Insert(entity);

            decimal applyUserWeight = 0;
            decimal otherWeight = 0;

            if (_IDCRepository.Table.Count() == 0)
                throw new Exception("请设置投资决策委员会成员！");

            var committeeCodes = _IDCRepository.Table.Select(x => x.Code).ToList();

            if(committeeCodes.Contains (entity.ApplyUser ))
            {
                applyUserWeight = otherWeight = 1 / committeeCodes.Count;
            }
            else
            {
                applyUserWeight = 0.35M;
                otherWeight = (1 - applyUserWeight) / committeeCodes.Count;
            }

            var defaultVoteInfos = new List<InvestmentDecisionVote>();
            foreach (var code in committeeCodes)
            {
                var info = new InvestmentDecisionVote
                {
                    AuthorityLevel = 0,
                    Flag = code == entity.ApplyUser ? (int)EnumLibrary.IDVoteFlag.Approval : (int)EnumLibrary.IDVoteFlag.None,
                    FormSerialNo = entity.SerialNo,
                    Reason = code == entity.ApplyUser ? "发起人默认赞同。" : string.Empty,
                    Type = code == entity.ApplyUser ?(int)EnumLibrary.IDVoteType .Applicant : (int) EnumLibrary.IDVoteType.Committee, 
                    UserCode = code,
                    VoteTime = _commonService.GetCurrentServerTime(),
                    Weight = code == entity.ApplyUser ? applyUserWeight : otherWeight,
                };

                defaultVoteInfos.Add(info);
            }
            _IDVRepository.Insert(defaultVoteInfos);
        }

        public virtual void DeleteInvestmentDecisionForm(IList<string> serialNos)
        {
            if (serialNos == null)
                throw new ArgumentNullException(nameof(serialNos));

            var forms = _IDFRepository.Table.Where(x => serialNos.Contains(x.SerialNo));

            _IDFRepository.Delete(forms.ToArray());

            var votes = _IDVRepository.Table.Where(x => serialNos.Contains(x.FormSerialNo));

            _IDVRepository.Delete(votes.ToArray());
        }

        public virtual InvestmentDecisionVote GetInvestmentDecisionVote(string investorCode, string formSerialNo)
        {
            if (investorCode == null)
                throw new ArgumentNullException(nameof(investorCode));

            if (formSerialNo == null)
                throw new ArgumentNullException(nameof(formSerialNo));

            var info = _IDVRepository.Table.SingleOrDefault(x => x.UserCode == investorCode && x.FormSerialNo == formSerialNo);

            return info;
        }

        public virtual void AddInvestmentDecisionVote(InvestmentDecisionVote entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _IDVRepository.Insert(entity);
        }

        public virtual void DeleteInvestmentDecisionVote(string investorCode, string formSerialNo)
        {
            if (investorCode == null)
                throw new ArgumentNullException(nameof(investorCode));

            if (formSerialNo == null)
                throw new ArgumentNullException(nameof(formSerialNo));

            var info = _IDVRepository.Table.SingleOrDefault(x => x.UserCode == investorCode && x.FormSerialNo == formSerialNo);

            _IDVRepository.Delete(info);
        }

        public virtual void UpdateInvestmentDecisionVote(InvestmentDecisionVote entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _IDVRepository.Update(entity);
        }

        public virtual IList<InvestmentDecisionVote> GetInvestmentDecisionVotes(string investorCode)
        {
            var query = _IDVRepository.Table.Where(x => x.UserCode == investorCode);

            return query.ToList();
        }

        public void InvestmentDecisionVoteProcess(string investorCode, string formSerialNo, EnumLibrary.IDVoteFlag flag, string reason)
        {
            var commanText = $@"EXEC [dbo].[sp_InvestmentDecisionVoteProcess]
                                        @InvestorCode = {investorCode},
		                                @FormSerialNo = {formSerialNo},
		                                @VoteFlag = {(int)flag},
		                                @Reason = {reason}";

            _dbContext.ExecuteSqlCommand(commanText); 

        }

        #endregion Methods
    }
}