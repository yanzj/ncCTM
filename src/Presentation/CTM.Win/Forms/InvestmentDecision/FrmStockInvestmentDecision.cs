﻿using System;
using System.Windows.Forms;
using CTM.Core.Infrastructure;
using CTM.Win.Util;

namespace CTM.Win.Forms.InvestmentDecision
{
    public partial class FrmStockInvestmentDecision : BaseForm
    {
        #region Fields

        private _embedIDApplication _progressingEmbedForm = null;
        private _embedIDApplication _doneEmbedForm = null;
        private _embedIDApplication _allEmbedForm = null;

        #endregion Fields

        #region Constructors

        public FrmStockInvestmentDecision()
        {
            InitializeComponent();
        }

        #endregion Constructors

        #region Utilities

        private void ShowEmbedIDApplication(DevExpress.XtraBars.Navigation.TabNavigationPage currentPage, _embedIDApplication embedForm)
        {
            if (embedForm == null)
                embedForm = EngineContext.Current.Resolve<_embedIDApplication>();

            embedForm.FormBorderStyle = FormBorderStyle.None;
            embedForm.TopLevel = false;
            currentPage.Controls.Add(embedForm);
            embedForm.Show();
        }

        #endregion Utilities

        #region Events

        private void FrmStockInvestmentDecision_Load(object sender, EventArgs e)
        {
            try
            {
                this.tabPane1.SelectedPage = this.tpProgressing;
            }
            catch (Exception ex)
            {
                DXMessage.ShowError(ex.Message);
            }
        }

        private void tabPane1_SelectedPageChanged(object sender, DevExpress.XtraBars.Navigation.SelectedPageChangedEventArgs e)
        {
            try
            {
                var currentPage = (DevExpress.XtraBars.Navigation.TabNavigationPage)e.Page;

                if (currentPage == this.tpProgressing)
                {
                    ShowEmbedIDApplication(currentPage, _progressingEmbedForm);
                }
                else if (currentPage == this.tpDone)
                {
                    ShowEmbedIDApplication(currentPage, _doneEmbedForm);
                }
                else if (currentPage == this.tpAll)
                {
                    ShowEmbedIDApplication(currentPage, _allEmbedForm);
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