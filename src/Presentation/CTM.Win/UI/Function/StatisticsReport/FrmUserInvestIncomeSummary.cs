﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using CTM.Core;
using CTM.Core.Util;
using CTM.Services.TradeRecord;
using CTM.Services.User;
using CTM.Win.Extensions;
using CTM.Win.Models;
using CTM.Win.Util;
using DevExpress.Utils;

namespace CTM.Win.UI.Function.StatisticsReport
{
    public partial class FrmUserInvestIncomeSummary : BaseForm
    {
        #region Fields

        private readonly IDailyRecordService _tradeRecordService;
        private readonly IUserService _userService;

        private readonly DateTime _initDate = AppConfigHelper.StatisticsInitDate;

        private const string _layoutXmlName = "FrmUserInvestIncomeSummary";

        #endregion Fields

        #region Constructors

        public FrmUserInvestIncomeSummary(IDailyRecordService tradeRecordService, IUserService userService)
        {
            InitializeComponent();

            this._tradeRecordService = tradeRecordService;
            this._userService = userService;
        }

        #endregion Constructors

        #region Utilities

        private void DisplaySearchResult()
        {
            var dateFrom = CommonHelper.StringToDateTime(this.deFrom.EditValue.ToString());
            var dateTo = CommonHelper.StringToDateTime(this.deTo.EditValue.ToString());

            var queryResult = CalculateUserIncomeSummary(dateFrom.AddDays(-1), dateTo);

            var totalSummary = CalculateTotalInvestIncome(queryResult);

            var result = DataFormat(totalSummary);

            this.gridControl1.DataSource = result;
        }

        private IList<UserInvestIncomeSummaryModel> DataFormat(IList<UserInvestIncomeSummaryModel> source)
        {
            var result = new List<UserInvestIncomeSummaryModel>();
            int unit = (int)EnumLibrary.NumericUnit.TenThousand;

            result = source.Select(x => new UserInvestIncomeSummaryModel()
            {
                Investor = x.Investor,

                TradeTypeName = CTMHelper.GetTradeTypeName(x.TradeType),

                StockFullCode = x.StockFullCode,
                StockName = x.StockName,

                AllotFund = x.Type == 0 ? 0 : CommonHelper.SetDecimalDigits(x.AllotFund / unit),
                AccumulatedProfit = CommonHelper.SetDecimalDigits(x.AccumulatedProfit / unit),
                AccumulatedIncomeRate = CommonHelper.SetDecimalDigits(x.AccumulatedIncomeRate, 4),

                InitAsset = x.Type == 0 ? 0 : CommonHelper.SetDecimalDigits(x.InitAsset / unit),
                InitHoldingVolume = x.Type == 0 ? x.InitHoldingVolume : 0,
                InitPositionValue = CommonHelper.SetDecimalDigits(x.InitPositionValue / unit),
                InitProfit = CommonHelper.SetDecimalDigits(x.InitProfit / unit),

                CurrentAsset = x.Type == 0 ? 0 : CommonHelper.SetDecimalDigits(x.CurrentAsset / unit),
                CurrentHoldingVolume = x.Type == 0 ? x.CurrentHoldingVolume : 0,
                CurrentPositionValue = CommonHelper.SetDecimalDigits(x.CurrentPositionValue / unit),
                CurrentPrice = x.Type == 0 ? CommonHelper.SetDecimalDigits(x.CurrentPrice) : 0,
                CurrentProfit = CommonHelper.SetDecimalDigits(x.CurrentProfit / unit),
                CurrentIncomeRate = CommonHelper.SetDecimalDigits(x.CurrentIncomeRate, 4),
            }
            ).OrderBy(x => x.Investor).ThenBy(x => x.StockFullCode).ThenBy(x => x.StockName).ToList();

            return result;
        }

        private IList<UserInvestIncomeSummaryModel> CalculateTotalInvestIncome(IList<UserInvestIncomeSummaryModel> queryResult)
        {
            var totalSummaryRecords = new List<UserInvestIncomeSummaryModel>();
            totalSummaryRecords.AddRange(queryResult);

            //投资人小计
            var recordsByInvestor = queryResult.GroupBy(x => x.Investor);

            foreach (var investorGroup in recordsByInvestor)
            {
                var firstRecord = investorGroup.First();
                var subTotalAccumulatedProfit = investorGroup.Sum(x => x.AccumulatedProfit);
                var subTotalInitProfit = investorGroup.Sum(x => x.InitProfit);
                var subTotalCurrentProfit = investorGroup.Sum(x => x.CurrentProfit);

                var subTotalModel = new UserInvestIncomeSummaryModel
                {
                    Type = 1,

                    Investor = firstRecord.Investor,

                    StockFullCode = string.Empty,
                    StockName = "小    计：",

                    AllotFund = firstRecord.AllotFund,
                    AccumulatedProfit = subTotalAccumulatedProfit,
                    AccumulatedIncomeRate = subTotalAccumulatedProfit / firstRecord.AllotFund,

                    InitAsset = subTotalInitProfit + firstRecord.AllotFund,
                    InitHoldingVolume = 0,
                    InitPositionValue = investorGroup.Sum(x => x.InitPositionValue),
                    InitProfit = investorGroup.Sum(x => x.InitProfit),

                    CurrentAsset = subTotalAccumulatedProfit + firstRecord.AllotFund,
                    CurrentHoldingVolume = 0,
                    CurrentPositionValue = investorGroup.Sum(x => x.CurrentPositionValue),
                    CurrentPrice = 0,
                    CurrentProfit = subTotalCurrentProfit,
                    CurrentIncomeRate = subTotalCurrentProfit / firstRecord.AllotFund,

                    TradeType = 0,
                };

                totalSummaryRecords.Add(subTotalModel);
            }

            var allSubTotalRecords = totalSummaryRecords.Where(x => x.Type == 1);

            var totalModel = new UserInvestIncomeSummaryModel();

            totalModel.Type = 2;

            totalModel.Investor = " 总    计： ";

            totalModel.StockName = string.Empty;
            totalModel.StockFullCode = string.Empty;

            totalModel.AllotFund = allSubTotalRecords.Sum(x => x.AllotFund);
            totalModel.AccumulatedProfit = allSubTotalRecords.Sum(x => x.AccumulatedProfit);
            totalModel.AccumulatedIncomeRate = totalModel.AccumulatedProfit / totalModel.AllotFund;

            totalModel.InitAsset = allSubTotalRecords.Sum(x => x.InitAsset);
            totalModel.InitHoldingVolume = 0;
            totalModel.InitPositionValue = allSubTotalRecords.Sum(x => x.InitPositionValue);
            totalModel.InitProfit = allSubTotalRecords.Sum(x => x.InitProfit);

            totalModel.CurrentAsset = allSubTotalRecords.Sum(x => x.CurrentAsset);
            totalModel.CurrentHoldingVolume = 0;
            totalModel.CurrentPositionValue = allSubTotalRecords.Sum(x => x.CurrentPositionValue);
            totalModel.CurrentPrice = 0;
            totalModel.CurrentProfit = allSubTotalRecords.Sum(x => x.CurrentProfit);
            totalModel.CurrentIncomeRate = totalModel.CurrentProfit / totalModel.AllotFund;

            totalModel.TradeType = 0;

            totalSummaryRecords.Add(totalModel);

            return totalSummaryRecords;
        }

        private IList<UserInvestIncomeSummaryModel> CalculateUserIncomeSummary(DateTime startDate, DateTime endDate)
        {
            var result = new List<UserInvestIncomeSummaryModel>();

            var beneficiaries = new string[1];
            if (LoginInfo.CurrentUser.IsAdmin)
                beneficiaries = null;
            else
                beneficiaries[0] = LoginInfo.CurrentUser.UserCode;

            var allRecords = _tradeRecordService.GetDailyRecords(beneficiaries: beneficiaries, tradeDateFrom: _initDate, tradeDateTo: endDate);

            if (!allRecords.Any()) return result;

            var stockFullCodes = allRecords.Select(x => x.StockCode).Distinct().ToArray();
            var queryDates = new List<DateTime> { startDate, endDate };
            var stockClosePrices = TKLineHelper.GetStockClosePrices(queryDates, stockFullCodes);
            var initDateClosePrices = stockClosePrices.Tables[startDate.ToString()];
            var currentDateClosePrices = stockClosePrices.Tables[endDate.ToString()];

            var allBeneficiaries = allRecords.Select(x => x.Beneficiary).Distinct().ToArray();
            var allBeneficiaryInfos = _userService.GetUserInfoByCode(allBeneficiaries);

            //受益人分组记录
            var recordsByBeneficiary = allRecords.GroupBy(x => x.Beneficiary);

            foreach (var beneficiaryGroup in recordsByBeneficiary)
            {
                var beneficiaryInfo = allBeneficiaryInfos.SingleOrDefault(x => x.Code == beneficiaryGroup.Key);
                if (beneficiaryInfo == null) continue;

                var investor = beneficiaryInfo.Name;
                var allotFund = beneficiaryInfo.AllotFund;

                //股票分组记录
                var recordsByStockCode = beneficiaryGroup.GroupBy(x => x.StockCode);

                foreach (var stockGroup in recordsByStockCode)
                {
                    var stockFullCode = stockGroup.Key;
                    var stockName = stockGroup.First().StockName;

                    var recordsByTradeType = stockGroup.GroupBy(x => x.TradeType);

                    //期初股票最新收盘价格
                    decimal initClosePrice = initDateClosePrices.AsEnumerable().Where(x => x.Field<string>("StockCode").Trim() == stockFullCode).Select(x => x.Field<decimal>("Close")).SingleOrDefault();

                    //期末股票最新收盘价格
                    decimal currentClosePrice = currentDateClosePrices.AsEnumerable().Where(x => x.Field<string>("StockCode").Trim() == stockFullCode).Select(x => x.Field<decimal>("Close")).SingleOrDefault();

                    foreach (var tradeTypeGroup in recordsByTradeType)
                    {
                        var tradeType = tradeTypeGroup.Key;

                        #region 期初处理

                        //截至期初的交易记录
                        var initRecords = tradeTypeGroup.Where(x => x.TradeDate <= startDate);

                        //发生金额
                        decimal initActualAmount = initRecords.Sum(x => x.ActualAmount);

                        //股票的持股数
                        int initHoldingVolume = initRecords.Sum(x => x.DealVolume);

                        //持仓市值
                        decimal initPositionValue = initHoldingVolume * initClosePrice;

                        //累计收益额
                        decimal initAccumulatedProfit = initActualAmount + initPositionValue;

                        #endregion 期初处理

                        #region 期末处理

                        //截至期末的交易记录
                        var currentRecords = tradeTypeGroup;

                        //股票持股数
                        int currentHoldingVolume = currentRecords.Sum(x => x.DealVolume);

                        //发生金额
                        decimal currentActualAmount = currentRecords.Sum(x => x.ActualAmount);

                        //持仓市值
                        decimal currentPositionValue = currentHoldingVolume * currentClosePrice;

                        //累计收益额
                        decimal currentAccumulatedProfit = currentActualAmount + currentPositionValue;

                        //累计收益率
                        decimal currentAccumulatedIncomeRate = 0.00M;

                        //本期收益
                        decimal currentProfit = currentAccumulatedProfit - initAccumulatedProfit;

                        //本期收益率
                        decimal currentIncomeRate = 0.00M;

                        if (tradeType == (int)EnumLibrary.TradeType.Day)
                        {
                            currentIncomeRate = currentPositionValue > allotFund ? currentProfit / currentPositionValue : currentProfit / allotFund;
                            currentAccumulatedIncomeRate = currentPositionValue > allotFund ? currentAccumulatedProfit / currentPositionValue : currentAccumulatedProfit / allotFund;
                        }
                        else
                        {
                            currentIncomeRate = currentProfit / allotFund;
                            currentAccumulatedIncomeRate = currentAccumulatedProfit / allotFund;
                        }

                        #endregion 期末处理

                        var tradeTypeSummaryModel = new UserInvestIncomeSummaryModel()
                        {
                            Type = 0,

                            Investor = investor,

                            StockFullCode = stockFullCode,
                            StockName = stockName,

                            AllotFund = allotFund,
                            AccumulatedProfit = currentAccumulatedProfit,
                            AccumulatedIncomeRate = currentAccumulatedIncomeRate,

                            InitAsset = allotFund + initAccumulatedProfit,
                            InitHoldingVolume = initHoldingVolume,
                            InitPositionValue = initPositionValue,
                            InitProfit = initAccumulatedProfit,

                            TradeType = tradeType,
                            CurrentAsset = allotFund + currentAccumulatedProfit,
                            CurrentPositionValue = currentPositionValue,
                            CurrentHoldingVolume = currentHoldingVolume,
                            CurrentPrice = currentClosePrice,
                            CurrentProfit = currentProfit,
                            CurrentIncomeRate = currentIncomeRate,
                        };

                        result.Add(tradeTypeSummaryModel);
                    }
                }
            }
            return result;
        }

        #endregion Utilities

        #region Events

        private void FrmUserInvestIncomeSummary_Load(object sender, EventArgs e)
        {
            this.deFrom.Properties.AllowNullInput = DefaultBoolean.False;
            this.deFrom.EditValue = _initDate;

            this.deTo.Properties.AllowNullInput = DefaultBoolean.False;
            var now = DateTime.Now;
            if (now.Hour < 15)
                this.deTo.EditValue = now.Date.AddDays(-1);
            else
                this.deTo.EditValue = now.Date;

            this.bandedGridView1.LoadLayout(_layoutXmlName);
            this.bandedGridView1.SetLayout(showCheckBoxRowSelect: false, showFilterPanel: true, showGroupPanel: true);

            this.ActiveControl = this.btnSearch;
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            var pph = new ProgressPanelHelper();

            try
            {
                this.btnSearch.Enabled = false;

                Thread progressPanelThread = pph.CreateProgressPanelThread();
                progressPanelThread.Start();

                DisplaySearchResult();
            }
            catch (Exception ex)
            {
                DXMessage.ShowError(ex.Message);
            }
            finally
            {
                pph.StopFlag = true;

                this.btnSearch.Enabled = true;
            }
        }

        private void btnSaveLayout_Click(object sender, EventArgs e)
        {
            this.bandedGridView1.SaveLayout(_layoutXmlName);
        }

        /// <summary>
        /// 显示数据行号
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bandedGridView1_CustomDrawRowIndicator(object sender, DevExpress.XtraGrid.Views.Grid.RowIndicatorCustomDrawEventArgs e)
        {
            if (e.Info.IsRowIndicator && e.RowHandle >= 0)
            {
                e.Info.DisplayText = (e.RowHandle + 1).ToString();
            }
        }

        #endregion Events
    }
}