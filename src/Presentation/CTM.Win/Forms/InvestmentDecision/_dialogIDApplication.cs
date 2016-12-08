﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CTM.Core;
using CTM.Core.Domain.InvestmentDecision;
using CTM.Core.Util;
using CTM.Services.Common;
using CTM.Services.InvestmentDecision;
using CTM.Services.Stock;
using CTM.Services.User;
using CTM.Win.Extensions;
using CTM.Win.Models;
using CTM.Win.Util;

namespace CTM.Win.Forms.InvestmentDecision
{
    public partial class _dialogIDApplication : BaseForm
    {
        #region Fields

        private readonly ICommonService _commonService;
        private readonly IInvestmentDecisionService _IDService;
        private readonly IStockService _stockService;
        private readonly IUserService _userService;
        private bool _initialFlag = true;

        #endregion Fields

        #region Delegates

        public delegate void RefreshParentForm();

        public event RefreshParentForm RefreshEvent;

        #endregion Delegates

        #region Constructors

        public _dialogIDApplication(
            ICommonService commonService,
            IInvestmentDecisionService IDService,
            IStockService stockService,
            IUserService userService)
        {
            InitializeComponent();

            this._commonService = commonService;
            this._IDService = IDService;
            this._stockService = stockService;
            this._userService = userService;
        }

        #endregion Constructors

        #region Utilities

        private void FormInit()
        {
            var now = _commonService.GetCurrentServerTime();

            //申请人员
            this.txtApplyUser.Text = LoginInfo.CurrentUser.UserName;

            //申请日期
            this.deApply.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.False;
            this.deApply.EditValue = now.Date;

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

            //操作类型
            var operateTypes = new List<ComboBoxItemModel>
            {
                new ComboBoxItemModel
                {
                    Text ="目标",
                    Value ="1",
                },
                new ComboBoxItemModel
                {
                    Text ="波段",
                    Value ="2",
                },
                       new ComboBoxItemModel
                {
                    Text ="隔日短差",
                    Value ="3",
                },
            };

            this.txtOperateUser.Text = LoginInfo.CurrentUser.UserName;
            this.cbOperateType.Initialize(operateTypes);
            this.cbOperateType.DefaultSelected("1");
            this.txtProfitPrice.SetNumericMask(2);
            this.txtProfitPrice.Text = string.Empty;
            this.spinProfitBound.SetProperties();
            this.txtLossPrice.SetNumericMask(2);
            this.txtLossPrice.Text = string.Empty;
            this.spinLossBound.SetProperties();

            //操作人员
            this.txtOperateUser.Text = LoginInfo.CurrentUser.UserName;

            //操作日期
            this.deOperate.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.False;
            this.deOperate.EditValue = now.Date;

            this.txtPrice.SetNumericMask(2);
            this.txtPrice.Text = string.Empty;
            this.spinPriceBound.SetProperties();
            this.txtVolume.SetNumberMask();
            this.txtVolume.Text = string.Empty;

            //理由类别
            var categories = _IDService.GetIDReasonCategories();

            this.treeListLookUpEdit1.Properties.DisplayMember = "Name";
            this.treeListLookUpEdit1.Properties.ValueMember = "Id";
            this.treeListLookUpEdit1TreeList.Initialize(categories, "Id", "ParentId", editable: false, autoWidth: true, showColumns: false, showVertLines: false, showHorzLines: false, multiSelect: true);
            categories = null;
        }

        private bool SubmitProcess()
        {
            if (string.IsNullOrEmpty(this.luStock.SelectedValue()))
            {
                DXMessage.ShowTips("请选择股票信息！");
                this.luStock.Focus();
                return false;
            }

            if (this.txtProfitPrice.Text.Trim().Length == 0)
            {
                DXMessage.ShowTips("请输入止盈价格！");
                this.txtProfitPrice.Focus();
                return false;
            }

            if (decimal.Parse(this.txtProfitPrice.Text.Trim()) <= 0)
            {
                DXMessage.ShowTips("止盈价格应该大于0！");
                this.txtProfitPrice.Focus();
                return false;
            }

            if (this.txtLossPrice.Text.Trim().Length == 0)
            {
                DXMessage.ShowTips("请输入止损价格！");
                this.txtLossPrice.Focus();
                return false;
            }

            if (decimal.Parse(this.txtLossPrice.Text.Trim()) <= 0)
            {
                DXMessage.ShowTips("止损价格应该大于0！");
                this.txtLossPrice.Focus();
                return false;
            }

            if (this.txtPrice.Text.Trim().Length == 0)
            {
                DXMessage.ShowTips("请输入单价！");
                this.txtPrice.Focus();
                return false;
            }

            if (decimal.Parse(this.txtPrice.Text.Trim()) <= 0)
            {
                DXMessage.ShowTips("单价应该大于0！");
                this.txtPrice.Focus();
                return false;
            }

            if (this.txtVolume.Text.Trim().Length == 0)
            {
                DXMessage.ShowTips("请输入数量！");
                this.txtVolume.Focus();
                return false;
            }

            if (decimal.Parse(this.txtVolume.Text.Trim()) <= 0)
            {
                DXMessage.ShowTips("数量应该大于0！");
                this.txtVolume.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(this.memoReason.Text.Trim()))
            {
                DXMessage.ShowTips("请输入申请理由！");
                this.memoReason.Focus();
                return false;
            }

            var applyDate = CommonHelper.StringToDateTime(this.deApply.EditValue.ToString());
            var tradeType = int.Parse(this.cbOperateType.SelectedValue());
            var stock = this.luStock.GetSelectedDataRow() as StockInfoModel;
            var now = _commonService.GetCurrentServerTime();
            var stopProfitPrice = decimal.Parse(this.txtProfitPrice.Text.Trim());
            var stopProfitBound = decimal.Parse(this.spinProfitBound.EditValue.ToString());
            var stopLossPrice = decimal.Parse(this.txtLossPrice.Text.Trim());
            var stopLossBound = decimal.Parse(this.spinLossBound.EditValue.ToString());

            var application = new InvestmentDecisionApplication
            {
                ApplyNo = string.Empty,
                ApplyDate = applyDate,
                ApplyUser = LoginInfo.CurrentUser.UserCode,
                CreateTime = now,
                DepartmentId = LoginInfo.CurrentUser.DepartmentId,
                Status = (int)EnumLibrary.IDApplicationStatus.Proceed,
                StockCode = stock.FullCode,
                StockName = stock.Name,
                StopLossBound = stopLossBound,
                StopLossPrice = stopLossPrice,
                StopProfitBound = stopProfitBound,
                StopProfitPrice = stopProfitPrice,
                TradePlanNo = txtPlanNo.Text.Trim(),
                TradeType = tradeType,
                UpdateTime = now,
            };

            var price = decimal.Parse(this.txtPrice.Text.Trim());
            var priceBound = decimal.Parse(this.spinPriceBound.EditValue.ToString());
            var volume = decimal.Parse(this.txtVolume.Text.Trim());
            var amount = Math.Abs(decimal.Parse(this.txtAmount.Text.Trim()) * (int)EnumLibrary.NumericUnit.TenThousand);

            var operation = new InvestmentDecisionOperation
            {
                AccuracyPoint = 0,
                AccuracyStatus = (int)EnumLibrary.IDOperationAccuracyStatus.None,
                ApplyNo = string.Empty,
                CreateTime = now,
                DealAmount = amount,
                DealFlag = chkBuy.Checked ? true : false,
                DealPrice = price,
                DealVolume = volume,
                ExecuteFlag = false,
                InitialFlag = _initialFlag,
                OperateUser = LoginInfo.CurrentUser.UserCode,
                OperateNo = string.Empty,
                PriceBound = priceBound,
                ReasonCategoryId = int.Parse(this.treeListLookUpEdit1.SelectedValue()),
                ReasonContent = memoReason.Text.Trim(),
                StockCode = stock.FullCode,
                StockName = stock.Name,
                TradeRecordRelateFlag = false,
                UpdateTime = now,
                VotePoint = 0,
                VoteStatus = (int)EnumLibrary.IDOperationVoteStatus.None,
            };

            _IDService.IDApplicationApplyProcess(application, operation);

            return true;
        }

        private void CalculateAmount()
        {
            if (!string.IsNullOrEmpty(this.txtVolume.Text.Trim()) && !string.IsNullOrEmpty(this.txtPrice.Text.Trim()))
            {
                decimal dealAmount = CommonHelper.SetDecimalDigits(decimal.Parse(this.txtVolume.Text.Trim()) * decimal.Parse(this.txtPrice.Text.Trim()) / (int)EnumLibrary.NumericUnit.TenThousand, 6);

                this.txtAmount.Text = dealAmount.ToString();
            }
        }

        private void CalculatePriceBound()
        {
            if (!string.IsNullOrEmpty(this.spinPriceBound.EditValue.ToString()) && !string.IsNullOrEmpty(this.txtPrice.Text.Trim()))
            {
                decimal dealUpBound = CommonHelper.SetDecimalDigits((1 + decimal.Parse(this.spinPriceBound.EditValue.ToString()) / (int)EnumLibrary.NumericUnit.Hundred) * decimal.Parse(this.txtPrice.Text.Trim()), 2);
                decimal dealDownBound = CommonHelper.SetDecimalDigits((1 - decimal.Parse(this.spinPriceBound.EditValue.ToString()) / (int)EnumLibrary.NumericUnit.Hundred) * decimal.Parse(this.txtPrice.Text.Trim()), 2);
                this.lblPriceBound.Text = dealDownBound.ToString() + " ~ " + dealUpBound.ToString();
            }
        }

        #endregion Utilities

        #region Events

        private void _dialogTradeApplication_Load(object sender, EventArgs e)
        {
            try
            {
                FormInit();
            }
            catch (Exception ex)
            {
                DXMessage.ShowError(ex.Message);
            }
        }

        private void deApply_EditValueChanged(object sender, EventArgs e)
        {
            var applyDate = CommonHelper.StringToDateTime(this.deApply.EditValue.ToString());
            // this.txtSerialNo.Text = _IDService.GenerateIDFSerialNo(applyDate);
        }

        private void chkBuy_CheckedChanged(object sender, EventArgs e)
        {
            this.chkSell.Checked = !this.chkBuy.Checked;
        }

        private void chkSell_CheckedChanged(object sender, EventArgs e)
        {
            this.chkBuy.Checked = !this.chkSell.Checked;
        }

        private void btnSubmit_Click(object sender, EventArgs e)
        {
            try
            {
                this.btnSubmit.Enabled = false;

                if (SubmitProcess())
                {
                    RefreshEvent?.Invoke();

                    this.Close();
                }
            }
            catch (Exception ex)
            {
                DXMessage.ShowError(ex.Message);
            }
            finally
            {
                this.btnSubmit.Enabled = true;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void txtPrice_EditValueChanged(object sender, EventArgs e)
        {
            CalculatePriceBound();
            CalculateAmount();
        }

        private void txtVolume_EditValueChanged(object sender, EventArgs e)
        {
            CalculateAmount();
        }

        private void spinPriceBound_EditValueChanged(object sender, EventArgs e)
        {
            CalculatePriceBound();
        }

        #endregion Events
    }
}