﻿namespace CTM.Core.Domain.InvestmentDecision
{
    public class InvestmentDecisionCommittee : BaseEntity
    {
        public string Code { get; set; }

        public string Name { get; set; }

        public decimal Weight { get; set; }
    }
}