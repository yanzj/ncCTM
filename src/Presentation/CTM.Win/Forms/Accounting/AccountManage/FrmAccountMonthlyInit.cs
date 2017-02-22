﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CTM.Core;
using CTM.Core.Domain.MonthlyProcess;
using CTM.Core.Util;
using CTM.Services.Account;
using CTM.Services.Common;
using CTM.Services.Dictionary;
using CTM.Services.MonthlyProcess;
using CTM.Services.Stock;
using CTM.Win.Extensions;
using CTM.Win.Models;
using CTM.Win.Util;

namespace CTM.Win.Forms.Accounting.AccountManage
{
    public partial class FrmAccountMonthlyInit : BaseForm
    {
        #region Fields

        private readonly IDictionaryService _dictionaryService;
        private readonly IAccountService _accountService;
        private readonly IStockService _stockService;
        private readonly ICommonService _commonService;
        private readonly IMonthEndProcessService _monthEndServer;

        private int _currentAccountId;
        private string _currentAccountCode;
        private string _currentAccountInfo;

        private IList<AccountEntity> _accountInfos = null;

        private int _yearMonth;

        #endregion Fields

        #region Constructors

        public FrmAccountMonthlyInit(
            IDictionaryService dictionaryService,
            IAccountService accountService,
            IStockService stockService,
            ICommonService commonService,
            IMonthEndProcessService monthEndServer)
        {
            InitializeComponent();

            this._dictionaryService = dictionaryService;
            this._accountService = accountService;
            this._stockService = stockService;
            this._commonService = commonService;
            this._monthEndServer = monthEndServer;
        }

        #endregion Constructors

        #region Utilities

        private void FormInit()
        {
            //账户名称
            var accountNames = _accountService.GetAllAccountNames(false).ToList();
            this.cbAccount.Initialize(accountNames, displayAdditionalItem: true);

            //账户属性
            var accountAttributes = _dictionaryService.GetDictionaryInfoByTypeId((int)EnumLibrary.DictionaryType.AccountAttribute)
            .Select(x => new ComboBoxItemModel
            {
                Value = x.Code.ToString(),
                Text = x.Name
            }).ToList();
            this.cbAttribute.Initialize(accountAttributes, displayAdditionalItem: true);

            //证券公司
            var securityCompanys = _dictionaryService.GetDictionaryInfoByTypeId((int)EnumLibrary.DictionaryType.SecurityCompay)

                        .Select(x => new ComboBoxItemModel
                        {
                            Value = x.Code.ToString(),
                            Text = x.Name
                        }).OrderBy(x => x.Text).ToList();

            this.cbSecurity.Initialize(securityCompanys, displayAdditionalItem: true);

            this.gridView1.SetLayout(showAutoFilterRow: false, showCheckBoxRowSelect: false);

            this.deInit.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.False;
            this.deInit.SetFormat("yyyy年MM月");

            var now = _commonService.GetCurrentServerTime().Date;

            if (now.AddDays(1).Month == now.Month + 1)
                now = now.AddMonths(1);

            this.deInit.EditValue = now;

            this.lciCancel.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
            this.lciEdit.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
            this.lciSave.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;

            //股票
            var stocks = _stockService.GetAllStocks(showDeleted: true)
                .Select(x => new StockInfoModel
                {
                    Id = x.Id,
                    Code = x.Code,
                    FullCode = x.FullCode,
                    Name = x.Name,
                    DisplayMember = x.FullCode + " - " + x.Name,
                }
           ).OrderBy(x => x.FullCode).ToList();
            this.luStock.Initialize(stocks, "FullCode", "DisplayMember", enableSearch: true);

            this.btnAdd.Enabled = false;

            this.gridView2.SetLayout(showAutoFilterRow: false, showCheckBoxRowSelect: false, editable: true, readOnly: false);

            foreach (DevExpress.XtraGrid.Columns.GridColumn column in this.gridView2.Columns)
            {
                column.OptionsColumn.AllowEdit = column.Name == this.colPositionVolume.Name;
            }
        }

        private void BindAccountList()
        {
            this.gridControl1.DataSource = null;

            if (_accountInfos == null)
                _accountInfos = _accountService.GetAccountDetails(showDisabled: false)
                   .OrderBy(x => x.Name).ThenBy(x => x.SecurityCompanyName).ThenBy(x => x.AttributeName).ToList();

            IList<AccountEntity> source = _accountInfos;

            var accountName = this.cbAccount.SelectedItem as string;
            if (!string.IsNullOrEmpty(accountName) && accountName != "全部")
                source = source.Where(x => x.Name == accountName).ToList();

            var securityCode = this.cbSecurity.SelectedValue();
            if (!string.IsNullOrEmpty(securityCode) && securityCode != "0")
                source = source.Where(x => x.SecurityCompanyCode == int.Parse(securityCode)).ToList();

            var attributeCode = this.cbAttribute.SelectedValue();
            if (!string.IsNullOrEmpty(attributeCode) && attributeCode != "0")
                source = source.Where(x => x.AttributeCode == int.Parse(attributeCode)).ToList();

            this.gridControl1.DataSource = source;
        }

        private void BindAccountMonthlyFund()
        {
            this.txtAccountInfo.EditValue = _currentAccountInfo;

            var fundInfo = _monthEndServer.GetAccountMonthlyFund(_currentAccountId, _yearMonth);

            if (fundInfo != null)
            {
                this.lciEdit.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                this.lciCancel.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                this.lciSave.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;

                this.txtTotalAsset.ReadOnly = true;
                this.txtAvailableFund.ReadOnly = true;
                this.txtPositionValue.ReadOnly = true;
                this.txtFinancingLimit.ReadOnly = true;
                this.txtFinancedAmount.ReadOnly = true;

                this.txtTotalAsset.EditValue = fundInfo.TotalAsset;
                this.txtAvailableFund.EditValue = fundInfo.AvailableFund;
                this.txtPositionValue.EditValue = fundInfo.PositionValue;
                this.txtFinancingLimit.EditValue = fundInfo.FinancingLimit;
                this.txtFinancedAmount.EditValue = fundInfo.FinancedAmount;
            }
            else
            {
                this.lciEdit.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
                this.lciCancel.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
                this.lciSave.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;

                this.txtTotalAsset.ReadOnly = false;
                this.txtAvailableFund.ReadOnly = false;
                this.txtPositionValue.ReadOnly = false;
                this.txtFinancingLimit.ReadOnly = false;
                this.txtFinancedAmount.ReadOnly = false;

                this.txtTotalAsset.EditValue = null;
                this.txtAvailableFund.EditValue = null;
                this.txtPositionValue.EditValue = null;
                this.txtFinancingLimit.EditValue = null;
                this.txtFinancedAmount.EditValue = null;
            }
        }

        private void BindAccountMonthlyPosition()
        {
            this.gridControl2.DataSource = null;

            var source = _monthEndServer.GetAccountMonthlyPosition(_currentAccountId, _yearMonth);

            this.gridControl2.DataSource = source;
        }

        private void SaveAccountMonthlyFund()
        {
            var fundInfo = new AccountMonthlyFund
            {
                AccountCode = _currentAccountCode,
                AccountId = _currentAccountId,
                AvailableFund = Convert.ToDecimal(this.txtAvailableFund.Text.Trim()),
                FinancedAmount = Convert.ToDecimal(this.txtFinancedAmount.Text.Trim()),
                FinancingLimit = Convert.ToDecimal(this.txtFinancingLimit.Text.Trim()),
                PositionValue = Convert.ToDecimal(this.txtPositionValue.Text.Trim()),
                TotalAsset = Convert.ToDecimal(this.txtTotalAsset.Text.Trim()),
                YearMonth = Convert.ToInt32(CommonHelper.StringToDateTime(this.deInit.EditValue.ToString()).ToString("yyyyMM")),
            };

            _monthEndServer.SaveAccountMonthlyFund(fundInfo);

            DXMessage.ShowTips("保存成功！");
        }

        #endregion Utilities

        #region Events

        private void FrmAccountMonthlyInit_Load(object sender, EventArgs e)
        {
            try
            {
                FormInit();

                BindAccountList();
            }
            catch (Exception ex)
            {
                DXMessage.ShowError(ex.Message);
            }
        }

        private void cbAccount_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindAccountList();
        }

        private void cbSecurity_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindAccountList();
        }

        private void cbAttribute_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindAccountList();
        }

        private void gridView1_CustomDrawRowIndicator(object sender, DevExpress.XtraGrid.Views.Grid.RowIndicatorCustomDrawEventArgs e)
        {
            if (e.Info.IsRowIndicator && e.RowHandle >= 0)
            {
                e.Info.DisplayText = (e.RowHandle + 1).ToString();
            }
        }

        private void gridView1_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            try
            {
                var gv = sender as DevExpress.XtraGrid.Views.Grid.GridView;

                var row = gv.GetRow(gv.FocusedRowHandle) as AccountEntity;

                if (row == null) return;

                _currentAccountId = row.Id;
                _currentAccountCode = row.Code;
                _currentAccountInfo = row.DisplayMember;

                _yearMonth = Convert.ToInt32(CommonHelper.StringToDateTime(this.deInit.EditValue.ToString()).ToString("yyyyMM"));

                this.luStock.EditValue = null;

                BindAccountMonthlyFund();

                BindAccountMonthlyPosition();
            }
            catch (Exception ex)
            {
                DXMessage.ShowError(ex.Message);
            }
        }

        private void deInit_EditValueChanged(object sender, EventArgs e)
        {
            try
            {
                if (this.gridView1.GetFocusedDataSourceRowIndex() < 0) return;

                _yearMonth = Convert.ToInt32(CommonHelper.StringToDateTime(this.deInit.EditValue.ToString()).ToString("yyyyMM"));

                BindAccountMonthlyFund();

                BindAccountMonthlyPosition();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void gridView2_CustomDrawRowIndicator(object sender, DevExpress.XtraGrid.Views.Grid.RowIndicatorCustomDrawEventArgs e)
        {
            if (e.Info.IsRowIndicator && e.RowHandle >= 0)
            {
                e.Info.DisplayText = (e.RowHandle + 1).ToString();
            }
        }

        private void gridView2_RowUpdated(object sender, DevExpress.XtraGrid.Views.Base.RowObjectEventArgs e)
        {
            try
            {
                DataRowView drv = (DataRowView)e.Row;
                DataRow row = drv.Row;
                if (row.RowState == DataRowState.Modified)
                {
                    var positionId = Convert.ToInt32(row[colPositionId.FieldName]);
                    var positionVolume = Convert.ToDecimal(row[colPositionVolume.FieldName]);

                    _monthEndServer.UpdateAccountMonthlyPosition(positionId, positionVolume);
                }
            }
            catch (Exception ex)
            {
                DXMessage.ShowError(ex.Message);
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            this.lciEdit.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;
            this.lciSave.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;
            this.lciCancel.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Always;

            this.txtTotalAsset.ReadOnly = false;
            this.txtAvailableFund.ReadOnly = false;
            this.txtPositionValue.ReadOnly = false;
            this.txtFinancingLimit.ReadOnly = false;
            this.txtFinancedAmount.ReadOnly = false;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                this.btnSave.Enabled = false;

                SaveAccountMonthlyFund();

                BindAccountMonthlyFund();
            }
            catch (Exception ex)
            {
                DXMessage.ShowError(ex.Message);
            }
            finally
            {
                this.btnSave.Enabled = true;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            try
            {
                BindAccountMonthlyFund();
            }
            catch (Exception ex)
            {
                DXMessage.ShowError(ex.Message);
            }
        }

        private void luStock_EditValueChanged(object sender, EventArgs e)
        {
            var stockCode = luStock.SelectedValue();

            this.btnAdd.Enabled = !string.IsNullOrEmpty(stockCode);
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                this.btnAdd.Enabled = false;

                var stockInfo = luStock.GetSelectedDataRow() as StockInfoModel;

                if (stockInfo == null) return;



                _monthEndServer.AddAccountMonthlyPosition(_currentAccountId, _currentAccountCode, _yearMonth, stockInfo.FullCode, stockInfo.Name);

                this.luStock.EditValue = null;

                BindAccountMonthlyPosition();
            }
            catch (Exception ex)
            {
                DXMessage.ShowError(ex.Message);
            }
        }

        private void repositoryItemButtonEdit1_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            try
            {
                e.Button.Enabled = false;

                var myView = this.gridView1;

                if (myView.FocusedRowHandle < 0) return;

                var positionId = Convert.ToInt32(myView.GetRowCellValue(myView.FocusedRowHandle, colPositionId));

                var buttonTag = e.Button.Tag.ToString().Trim();

                if (string.IsNullOrEmpty(buttonTag)) return;

                if (buttonTag == "Delete")
                {
                    if (DXMessage.ShowYesNoAndWarning("确定删除该股票持仓信息吗？") == System.Windows.Forms.DialogResult.Yes)
                    {
                        this._monthEndServer.DeleteAccountMonthlyPosition(positionId);

                        BindAccountMonthlyPosition();
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessage.ShowError(ex.Message);
            }
            finally
            {
                e.Button.Enabled = true;
            }
        }

        #endregion Events
    }
}