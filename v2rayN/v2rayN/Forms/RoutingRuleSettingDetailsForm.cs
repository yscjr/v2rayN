using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using v2rayN.Base;
using v2rayN.Handler;
using v2rayN.Mode;
using v2rayN.Resx;

namespace v2rayN.Forms
{
    public partial class RoutingRuleSettingDetailsForm : BaseForm
    {
        public RulesItem rulesItem
        {
            get; set;
        }

        public RoutingRuleSettingDetailsForm()
        {
            InitializeComponent();
            BingSourceData();
        }

        private void RoutingRuleSettingDetailsForm_Load(object sender, EventArgs e)
        {
            if (Utils.IsNullOrEmpty(rulesItem.outboundTag))
            {
                ClearBind();
            }
            else
            {
                BindingData();
            }
        }

        private void BingSourceData()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("boundTag");
            dt.Columns.Add("name");
            dt.Columns.Add("boundIndexId");
            //新建行的赋值
            dt.Rows.Add("proxy", "proxy", "proxy");
            dt.Rows.Add("direct", "direct", "direct");
            dt.Rows.Add("block", "block", "block");

            Font font = this.cmbOutboundTag.Font;
            Graphics g = this.cmbOutboundTag.CreateGraphics();
            int vertScrollBarWidth =
                (this.cmbOutboundTag.Items.Count + config.vmess.Count > this.cmbOutboundTag.MaxDropDownItems)
                ? SystemInformation.VerticalScrollBarWidth : 0;

            string tagName = null;
            int index = 1;
            int newWidth = 0;
            int width = this.cmbOutboundTag.DropDownWidth;
            foreach (var vmessitem in config.vmess)
            {

                //var vmessitem = config.vmess.Find(item => item.indexId.Equals(indexId));
                if (vmessitem == null || vmessitem.configType == EConfigType.Custom)
                {
                    //...
                    continue;
                }
                tagName = $"[{(vmessitem.indexId.Equals(config.indexId) ? "proxy" : index)}]{vmessitem.remarks}";
                newWidth = (int)g.MeasureString(tagName, font).Width + vertScrollBarWidth + 15;
                if (width < newWidth)
                    width = newWidth;   //set the width of the drop down list to the width of the largest item.
                dt.Rows.Add($"proxy{index}", tagName, vmessitem.indexId);
                index++;
            }
            this.cmbOutboundTag.DropDownWidth = width;
            //显示的数据

            this.cmbOutboundTag.DisplayMember = "name";//name为DataTable的字段名

            //隐藏的数据(对于多个数据，可以用逗号隔开。例：id，name)
            this.cmbOutboundTag.ValueMember = "boundIndexId";//id为DataTable的字段名(对于隐藏对个数据，把数据放到一个字段用逗号隔开)

            //绑定数据源
            this.cmbOutboundTag.DataSource = dt;
        }

        private void EndBindingData()
        {
            if (rulesItem != null)
            {
                rulesItem.port = txtPort.Text.TrimEx();

                var inboundTag = new List<String>();
                for (int i = 0; i < clbInboundTag.Items.Count; i++)
                {
                    if (clbInboundTag.GetItemChecked(i))
                    {
                        inboundTag.Add(clbInboundTag.Items[i].ToString());
                    }
                }
                rulesItem.inboundTag = inboundTag;

                var boundTag = ((DataRowView)cmbOutboundTag.SelectedItem).Row;
                rulesItem.outboundTag = boundTag[0].ToString();
                rulesItem.boundIndexId = boundTag[2].ToString();
                //rulesItem.outboundTag = cmbOutboundTag.Text;

                if (chkAutoSort.Checked)
                {
                    rulesItem.domain = Utils.String2ListSorted(txtDomain.Text);
                    rulesItem.ip = Utils.String2ListSorted(txtIP.Text);
                }
                else
                {
                    rulesItem.domain = Utils.String2List(txtDomain.Text);
                    rulesItem.ip = Utils.String2List(txtIP.Text);
                }

                var protocol = new List<string>();
                for (int i = 0; i < clbProtocol.Items.Count; i++)
                {
                    if (clbProtocol.GetItemChecked(i))
                    {
                        protocol.Add(clbProtocol.Items[i].ToString());
                    }
                }
                rulesItem.protocol = protocol;
                rulesItem.enabled = chkEnabled.Checked;
            }
        }
        private void BindingData()
        {
            if (rulesItem != null)
            {
                string boundIndexId = rulesItem.outboundTag;
                if (!(boundIndexId.Equals("proxy") || boundIndexId.Equals("direct") || boundIndexId.Equals("block")))
                {
                    boundIndexId = rulesItem.boundIndexId ?? ""; //当boundIndexId为null则赋值为""
                }
                cmbOutboundTag.SelectedValue = boundIndexId;

                txtPort.Text = rulesItem.port ?? string.Empty;
                //cmbOutboundTag.Text = rulesItem.outboundTag;
                txtDomain.Text = Utils.List2String(rulesItem.domain, true);
                txtIP.Text = Utils.List2String(rulesItem.ip, true);

                if (rulesItem.inboundTag != null)
                {
                    for (int i = 0; i < clbInboundTag.Items.Count; i++)
                    {
                        if (rulesItem.inboundTag.Contains(clbInboundTag.Items[i].ToString()))
                        {
                            clbInboundTag.SetItemChecked(i, true);
                        }
                    }
                }

                if (rulesItem.protocol != null)
                {
                    for (int i = 0; i < clbProtocol.Items.Count; i++)
                    {
                        if (rulesItem.protocol.Contains(clbProtocol.Items[i].ToString()))
                        {
                            clbProtocol.SetItemChecked(i, true);
                        }
                    }
                }
                chkEnabled.Checked = rulesItem.enabled;
            }
        }
        private void ClearBind()
        {
            txtPort.Text = string.Empty;
            cmbOutboundTag.Text = Global.agentTag;
            txtDomain.Text = string.Empty;
            txtIP.Text = string.Empty;
            chkEnabled.Checked = true;
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            EndBindingData();

            bool hasRule = 
                rulesItem.domain != null 
                && rulesItem.domain.Count > 0 
                || rulesItem.ip != null 
                && rulesItem.ip.Count > 0 
                || rulesItem.protocol != null 
                && rulesItem.protocol.Count > 0 
                || !Utils.IsNullOrEmpty(rulesItem.port);

            if (!hasRule)
            {
                UI.ShowWarning(string.Format(ResUI.RoutingRuleDetailRequiredTips, "Port/Protocol/Domain/IP"));
                return;
            }

            DialogResult = DialogResult.OK;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void linkRuleobjectDoc_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://www.v2fly.org/config/routing.html#ruleobject");
        }
    }
}
