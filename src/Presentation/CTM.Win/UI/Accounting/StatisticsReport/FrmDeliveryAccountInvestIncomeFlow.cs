﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using CTM.Core;
using CTM.Core.Domain.Account;
using CTM.Core.Util;
using CTM.Services.Account;
using CTM.Services.Dictionary;
using CTM.Services.StatisticsReport;
using CTM.Services.TKLine;
using CTM.Services.TradeRecord;
using CTM.Services.User;
using CTM.Win.Extensions;
using CTM.Win.Models;
using CTM.Win.Util;
using DevExpress.Utils;

namespace CTM.Win.UI.Accounting.StatisticsReport
{
    public partial class FrmDeliveryAccountInvestIncomeFlow : BaseForm
    {
        #region Fields

        private readonly IDeliveryRecordService _deliveryRecordService;
        private readonly IAccountService _accountService;
        private readonly IUserService _userService;
        private readonly IDictionaryService _dictionaryService;
        private readonly ITKLineService _tkLineService;
        private readonly IDeliveryStatisticsReportService _deliveryReportService;

        private readonly DateTime _initDate = AppConfigHelper.StatisticsInitDate;

        //查询交易日数量
        private const int _tradeDateNumber = 25;

        private const string _layoutXmlName = "FrmDeliveryAccountInvestIncomeFlow";

        #endregion Fields

        #region Constructors

        public FrmDeliveryAccountInvestIncomeFlow(
            IDeliveryRecordService deliveryRecordService,
            IAccountService accountService,
            IUserService userService,
            IDictionaryService dictionaryService,
            ITKLineService tkLineService,
            IDeliveryStatisticsReportService deliveryReportService
            )
        {
            InitializeComponent();

            this._deliveryRecordService = deliveryRecordService;
            this._accountService = accountService;
            this._userService = userService;
            this._dictionaryService = dictionaryService;
            this._tkLineService = tkLineService;
            this._deliveryReportService = deliveryReportService;
        }

        #endregion Constructors

        #region Utilities

        private void BindSearchInfo()
        {
            //开始交易日
            deStartTradeDate.Properties.AllowNullInput = DefaultBoolean.False;
            deStartTradeDate.EditValue = _initDate;

            //截至交易日
            this.deEndTradeDate.Properties.AllowNullInput = DefaultBoolean.False;

            var now = DateTime.Now;
            if (now.Hour < 15)
                this.deEndTradeDate.EditValue = now.Date.AddDays(-1);
            else
                this.deEndTradeDate.EditValue = now.Date;

            //账户类别
            var accountType = _dictionaryService.GetDictionaryInfoByTypeId((int)EnumLibrary.DictionaryType.AccountType)
                .Select(x => new ComboBoxItemModel
                {
                    Text = x.Name,
                    Value = x.Code.ToString(),
                }).ToList();
            cbAccountType.Initialize(accountType, displayAdditionalItem: true);
            cbAccountType.DefaultSelected("0");
        }

        private void BindAccountInfo(int accountType)
        {
            //账户
            var accounts = new List<AccountEntity>();

            var infos = _accountService.GetAccountInfos(typeCode: accountType, onlyNeedAccounting: true, showDisabled: true)
                  .Select(x => new AccountEntity
                  {
                      AccountId = x.Id,
                      AccountName = x.Name,
                      AttributeName = x.AttributeName,
                      InvestFund = x.InvestFund,
                      SecurityCompanyName = x.SecurityCompanyName,
                      TypeName = x.TypeName,
                      DisplayMember = x.Name + " - " + x.SecurityCompanyName + " - " + x.AttributeName + " - " + x.TypeName,
                  }
                 )
                .ToList();

            accounts.AddRange(infos);
            if (accounts.Count > 0)
            {
                //添加全选
                var all = new AccountEntity
                {
                    AccountId = 0,
                    AccountName = " 全部 ",
                    AttributeName = " 全部 ",
                    SecurityCompanyName = " 全部 ",
                    TypeName = this.cbAccountType.Text,
                    DisplayMember = " 全部 ",
                };
                accounts.Add(all);
            }

            accounts = accounts.OrderBy(x => x.AccountName).ThenBy(x => x.SecurityCompanyName).ToList();

            luAccount.Initialize(accounts, "AccountId", "DisplayMember", showHeader: true, enableSearch: true);
            luAccount.EditValue = 0;
        }

        private void AccessControl()
        {
            if (!LoginInfo.CurrentUser.IsAdmin)
            {
                this.cbAccountType.ReadOnly = true;
                this.cbAccountType.DefaultSelected("3");
            }
        }

        private void GetSearchResult()
        {
            this.gridControl1.DataSource = null;

            //查询开始交易日
            var startDate = CommonHelper.StringToDateTime(deStartTradeDate.EditValue.ToString());

            //查询截至交易日
            var endDate = CommonHelper.StringToDateTime(deEndTradeDate.EditValue.ToString());

            //选择的帐户信息
            var selectedAccount = this.luAccount.GetSelectedDataRow() as AccountEntity;

            if (selectedAccount == null) return;

            var accounts = new List<AccountEntity>();

            if (selectedAccount.AccountId == 0)
            {
                accounts = this.luAccount.Properties.DataSource as List<AccountEntity>;
                selectedAccount.InvestFund = accounts.Sum(x => x.InvestFund);
            }
            else
                accounts.Add(selectedAccount);

            var accountIds = accounts.Select(x => x.AccountId).ToArray();

            //交易记录
            var records = _deliveryRecordService.GetDeliveryRecords(accountIds: accountIds, tradeDateFrom: startDate, tradeDateTo: endDate).ToList();

            if (records?.Count == 0) return;

            //取得26个交易日日期
            var queryDates = CommonHelper.GetWorkdaysBeforeCurrentDay(endDate, _tradeDateNumber + 1).OrderBy(x => x).ToList();
            //交易记录中的所有股票代码
            var stockFullCodes = records.Select(x => x.StockCode).Distinct().ToArray();
            //各交易日所有股票收盘价
            var stockClosePrices = this._tkLineService.GetStockClosePrices(queryDates, stockFullCodes);

            var queryResult = this._deliveryReportService.CalculateAccountInvestIncome(records, queryDates, stockClosePrices, selectedAccount);

            var source = queryResult.Select(x => new AccountInvestIncomeEntity
            {
                AccountAttributeName = x.AccountAttributeName,
                AccountName = x.AccountName,
                AccountTypeName = x.AccountTypeName,
                AccumulatedIncomeRate = CommonHelper.SetDecimalDigits(x.AccumulatedIncomeRate, 4),
                AccumulatedProfit = CommonHelper.SetDecimalDigits(x.AccumulatedProfit),
                AllotFund = CommonHelper.SetDecimalDigits(x.AllotFund),
                CurrentAsset = CommonHelper.SetDecimalDigits(x.CurrentAsset),
                CurrentIncomeRate = CommonHelper.SetDecimalDigits(x.CurrentIncomeRate, 4),
                CurrentProfit = CommonHelper.SetDecimalDigits(x.CurrentProfit),
                FundOccupyAmount = CommonHelper.SetDecimalDigits(x.FundOccupyAmount),
                MondayPositionValue = CommonHelper.SetDecimalDigits(x.MondayPositionValue),
                PositionRate = CommonHelper.SetDecimalDigits(x.PositionRate, 4),
                PositionValue = CommonHelper.SetDecimalDigits(x.PositionValue),
                SecurityCompanyName = x.SecurityCompanyName,
                TradeTime = x.TradeTime,
            }
            );

            this.gridControl1.DataSource = source;
        }

        /// <summary>
        /// 取得账户投入资金
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        private decimal GetAccountInvestFund(int accountId)
        {
            decimal investFund = 0;

            if (accountId == 0)
            {
                var accountInfos = _accountService.GetAccountInfos();
                investFund = accountInfos.Sum(x => x.InvestFund);
            }
            else
            {
                var accountInfo = _accountService.GetAccountInfoById(accountId);
                if (accountInfo != null)
                    investFund = accountInfo.InvestFund;
            }

            return investFund;
        }

        #endregion Utilities

        #region Events

        private void FrmAccountInvestIncomeFlow_Load(object sender, EventArgs e)
        {
            BindSearchInfo();

            AccessControl();

            this.gridView1.LoadLayout(_layoutXmlName);
            this.gridView1.SetLayout(showAutoFilterRow: false, showCheckBoxRowSelect: false);

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

                GetSearchResult();
            }
            catch (Exception ex)
            {
                DXMessage.ShowError(ex.Message);
                this.gridControl1.DataSource = null;
            }
            finally
            {
                pph.StopFlag = true;

                this.btnSearch.Enabled = true;
            }
        }

        private void btnSaveLayout_Click(object sender, EventArgs e)
        {
            this.gridView1.SaveLayout(_layoutXmlName);
        }

        private void cbAccountType_SelectedIndexChanged(object sender, EventArgs e)
        {
            var accountType = int.Parse(this.cbAccountType.SelectedValue());

            BindAccountInfo(accountType);
        }

        /// <summary>
        /// 显示数据行号
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void gridView1_CustomDrawRowIndicator(object sender, DevExpress.XtraGrid.Views.Grid.RowIndicatorCustomDrawEventArgs e)
        {
            if (e.Info.IsRowIndicator && e.RowHandle >= 0)
            {
                e.Info.DisplayText = (e.RowHandle + 1).ToString();
            }
        }

        #endregion Events
    }
}