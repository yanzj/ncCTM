﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CTM.Core;
using CTM.Data;
using CTM.Services.Account;
using CTM.Services.Dictionary;
using CTM.Services.TradeRecord;
using CTM.Win.Extensions;
using CTM.Win.Models;
using CTM.Win.Util;

namespace CTM.Win.Forms.Accounting.DataManage
{
    public partial class _dialogTradeDataContrast : BaseForm
    {
        #region Fields

        private readonly IAccountService _accountService;
        private readonly IDictionaryService _dictionaryService;
        private readonly IDeliveryRecordService _deliveryService;

        private readonly string _connString = System.Configuration.ConfigurationManager.ConnectionStrings["CTMContext"].ToString();

        #endregion Fields

        #region Properties

        public int AccountId { get; set; }

        public string AccountInfo { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public string StockCode { get; set; }

        public string StockName { get; set; }

        public bool DealFlag { get; set; }

        #endregion Properties

        #region Constructors

        public _dialogTradeDataContrast(IAccountService accountService, IDictionaryService dictionaryService, IDeliveryRecordService deliveryService)
        {
            InitializeComponent();

            this._accountService = accountService;
            this._dictionaryService = dictionaryService;
            this._deliveryService = deliveryService;
        }

        #endregion Constructors

        #region Utilities

        private void FormInit()
        {
            this.esiTitle.Text = $@"{FromDate.ToShortDateString()} - {ToDate.ToShortDateString()}  [{AccountInfo}] - [{StockCode} - {StockName}] ";

            if (LoginInfo.CurrentUser.IsAdmin)
            {
                //实际受益人
                var dealers = _accountService.GetAccountOperatorsByAccountId(AccountId);
                this.luBeneficiary.Initialize(dealers, "Code", "Name", showHeader: false, showFooter: false);

                //交易类别
                var tradeTypes = this._dictionaryService.GetDictionaryInfoByTypeId((int)EnumLibrary.DictionaryType.TradeType)
                    .Select(x => new ComboBoxItemModel
                    {
                        Text = x.Name,
                        Value = x.Code.ToString(),
                    }).ToList();
                this.cbTradeType.Initialize(tradeTypes);
            }
            else
            {
                this.luBeneficiary.Enabled = false;
                this.cbTradeType.Enabled = false;
            }

            this.btnCopy.Enabled = false;

            this.gridView1.SetLayout(showAutoFilterRow: false, showCheckBoxRowSelect: LoginInfo.CurrentUser.IsAdmin, rowIndicatorWidth: 40);
            this.gridView2.SetLayout(showAutoFilterRow: false, showCheckBoxRowSelect: false, rowIndicatorWidth: 40);
        }

        private void BindTradeDate()
        {
            var commandText = $@"EXEC [dbo].[sp_GetDeliveryAndDailyContrastData] @AccountId = {AccountId} , @StockCode = '{StockCode}' , @FromDate = '{FromDate}' , @ToDate = '{ToDate}' , @DealFlag = {DealFlag}";

            var ds = SqlHelper.ExecuteDataset(_connString, CommandType.Text, commandText);

            if (ds == null || ds.Tables.Count == 0) return;

            this.gridControl1.DataSource = ds.Tables[0];

            this.gridControl2.DataSource = ds.Tables[1];
        }

        private void CopyProcess()
        {
            var beneficiary = luBeneficiary.SelectedValue();
            var tradeType = Convert.ToInt32(cbTradeType.SelectedValue());

            var deliveryRecordIds = new List<int>();
            var selectedHandles = this.gridView1.GetSelectedRows();
            for (var rowhandle = 0; rowhandle < selectedHandles.Length; rowhandle++)
            {
                deliveryRecordIds.Add(int.Parse(this.gridView1.GetRowCellValue(selectedHandles[rowhandle], colId_L).ToString()));
            }

            _deliveryService.CopyToDailyRecord(deliveryRecordIds, LoginInfo.CurrentUser.UserCode, AccountId, beneficiary, tradeType);
        }

        #endregion Utilities

        #region Events

        private void _dialogTradeDataContrast_Load(object sender, EventArgs e)
        {
            try
            {
                FormInit();
                BindTradeDate();
            }
            catch (Exception ex)
            {
                DXMessage.ShowError(ex.Message);
            }
        }

        private void gridView1_CustomDrawRowIndicator(object sender, DevExpress.XtraGrid.Views.Grid.RowIndicatorCustomDrawEventArgs e)
        {
            if (e.Info.IsRowIndicator && e.RowHandle >= 0)
            {
                e.Info.DisplayText = (e.RowHandle + 1).ToString();
            }
        }

        private void gridView2_CustomDrawRowIndicator(object sender, DevExpress.XtraGrid.Views.Grid.RowIndicatorCustomDrawEventArgs e)
        {
            if (e.Info.IsRowIndicator && e.RowHandle >= 0)
            {
                e.Info.DisplayText = (e.RowHandle + 1).ToString();
            }
        }

        private void gridView1_SelectionChanged(object sender, DevExpress.Data.SelectionChangedEventArgs e)
        {
            try
            {
                var myView = this.gridView1;
                var selectedHandles = myView.GetSelectedRows();
                if (selectedHandles.Any())
                    selectedHandles = selectedHandles.Where(x => x > -1).ToArray();

                if (selectedHandles.Length == 0)
                {
                    this.btnCopy.Enabled = false;
                }
                else
                {
                    btnCopy.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                DXMessage.ShowError(ex.Message);
            }
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(this.luBeneficiary.SelectedValue()))
                {
                    DXMessage.ShowTips("请选择实际受益人！");
                    return;
                }
                if (string.IsNullOrEmpty(this.cbTradeType.SelectedValue()))
                {
                    DXMessage.ShowTips("请选择交易类别！");
                    return;
                }

                if (DXMessage.ShowYesNoAndTips("是否确定导入？") == System.Windows.Forms.DialogResult.Yes)
                {
                    CopyProcess();

                    BindTradeDate();
                }
            }
            catch (Exception ex)
            {
                DXMessage.ShowError(ex.Message);
            }
        }

        #endregion Events
    }
}