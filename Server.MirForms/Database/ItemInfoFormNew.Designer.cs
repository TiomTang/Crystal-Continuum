
namespace Server.Database
{
    partial class ItemInfoFormNew
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            itemInfoGridView = new System.Windows.Forms.DataGridView();
            Modified = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            ItemIndex = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ItemName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ItemType = new System.Windows.Forms.DataGridViewComboBoxColumn();
            ItemGrade = new System.Windows.Forms.DataGridViewComboBoxColumn();
            ItemRequiredType = new System.Windows.Forms.DataGridViewComboBoxColumn();
            ItemRequiredGender = new System.Windows.Forms.DataGridViewComboBoxColumn();
            ItemRequiredClass = new System.Windows.Forms.DataGridViewComboBoxColumn();
            ItemSet = new System.Windows.Forms.DataGridViewComboBoxColumn();
            ItemRandomStatsId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ItemRequiredAmount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ItemImage = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ItemShape = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ItemEffect = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ItemStackSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ItemSlots = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ItemWeight = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ItemLightRange = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ItemLightIntensity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ItemDurability = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ItemPrice = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ItemToolTip = new System.Windows.Forms.DataGridViewTextBoxColumn();
            StartItem = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            NeedIdentify = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            ShowGroupPickup = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            GlobalDropNotify = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            ClassBased = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            LevelBased = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            CanMine = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            CanFastRun = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            CanAwakening = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            panel1 = new System.Windows.Forms.Panel();
            panel3 = new System.Windows.Forms.Panel();
            Gameshop_button = new System.Windows.Forms.Button();
            btnExport = new System.Windows.Forms.Button();
            btnImport = new System.Windows.Forms.Button();
            lblFilterType = new System.Windows.Forms.Label();
            groupView = new System.Windows.Forms.GroupBox();
            rBtnViewSpecial = new System.Windows.Forms.RadioButton();
            rbtnViewAll = new System.Windows.Forms.RadioButton();
            rbtnViewBinding = new System.Windows.Forms.RadioButton();
            rbtnViewBasic = new System.Windows.Forms.RadioButton();
            rbtnViewStats = new System.Windows.Forms.RadioButton();
            drpFilterType = new System.Windows.Forms.ComboBox();
            lblSearch = new System.Windows.Forms.Label();
            txtSearch = new System.Windows.Forms.TextBox();
            panel2 = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)itemInfoGridView).BeginInit();
            panel1.SuspendLayout();
            panel3.SuspendLayout();
            groupView.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // itemInfoGridView
            // 
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            itemInfoGridView.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            itemInfoGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            itemInfoGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { Modified, ItemIndex, ItemName, ItemType, ItemGrade, ItemRequiredType, ItemRequiredGender, ItemRequiredClass, ItemSet, ItemRandomStatsId, ItemRequiredAmount, ItemImage, ItemShape, ItemEffect, ItemStackSize, ItemSlots, ItemWeight, ItemLightRange, ItemLightIntensity, ItemDurability, ItemPrice, ItemToolTip, StartItem, NeedIdentify, ShowGroupPickup, GlobalDropNotify, ClassBased, LevelBased, CanMine, CanFastRun, CanAwakening });
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            itemInfoGridView.DefaultCellStyle = dataGridViewCellStyle3;
            itemInfoGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            itemInfoGridView.Location = new System.Drawing.Point(0, 0);
            itemInfoGridView.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            itemInfoGridView.Name = "itemInfoGridView";
            itemInfoGridView.RowTemplate.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            itemInfoGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            itemInfoGridView.Size = new System.Drawing.Size(1196, 500);
            itemInfoGridView.TabIndex = 0;
            itemInfoGridView.CurrentCellDirtyStateChanged += CurrentCellDirtyStateChanged;
            itemInfoGridView.DataError += itemInfoGridView_DataError;
            itemInfoGridView.DefaultValuesNeeded += itemInfoGridView_DefaultValuesNeeded;
            itemInfoGridView.UserDeletingRow += itemInfoGridView_UserDeletingRow;
            // 
            // Modified
            // 
            Modified.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            Modified.DataPropertyName = "Modified";
            Modified.Frozen = true;
            Modified.HeaderText = "Modified";
            Modified.Name = "Modified";
            Modified.ReadOnly = true;
            Modified.Width = 67;
            // 
            // ItemIndex
            // 
            ItemIndex.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            ItemIndex.DataPropertyName = "ItemIndex";
            ItemIndex.Frozen = true;
            ItemIndex.HeaderText = "Index";
            ItemIndex.Name = "ItemIndex";
            ItemIndex.ReadOnly = true;
            ItemIndex.Width = 65;
            // 
            // ItemName
            // 
            ItemName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            ItemName.DataPropertyName = "ItemName";
            ItemName.Frozen = true;
            ItemName.HeaderText = "Name";
            ItemName.Name = "ItemName";
            ItemName.Width = 68;
            // 
            // ItemType
            // 
            ItemType.DataPropertyName = "ItemType";
            ItemType.HeaderText = "Type";
            ItemType.Name = "ItemType";
            ItemType.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            ItemType.Width = 100;
            // 
            // ItemGrade
            // 
            ItemGrade.DataPropertyName = "ItemGrade";
            ItemGrade.HeaderText = "Grade";
            ItemGrade.Name = "ItemGrade";
            ItemGrade.Width = 100;
            // 
            // ItemRequiredType
            // 
            ItemRequiredType.DataPropertyName = "ItemRequiredType";
            ItemRequiredType.HeaderText = "Required Type";
            ItemRequiredType.Name = "ItemRequiredType";
            ItemRequiredType.Width = 100;
            // 
            // ItemRequiredGender
            // 
            ItemRequiredGender.DataPropertyName = "ItemRequiredGender";
            ItemRequiredGender.HeaderText = "Required Gender";
            ItemRequiredGender.Name = "ItemRequiredGender";
            ItemRequiredGender.Width = 100;
            // 
            // ItemRequiredClass
            // 
            ItemRequiredClass.DataPropertyName = "ItemRequiredClass";
            ItemRequiredClass.HeaderText = "Required Class";
            ItemRequiredClass.Name = "ItemRequiredClass";
            ItemRequiredClass.Width = 100;
            // 
            // ItemSet
            // 
            ItemSet.DataPropertyName = "ItemSet";
            ItemSet.HeaderText = "Set";
            ItemSet.Name = "ItemSet";
            ItemSet.Width = 100;
            // 
            // ItemRandomStatsId
            // 
            ItemRandomStatsId.DataPropertyName = "ItemRandomStatsId";
            ItemRandomStatsId.HeaderText = "Random Stats";
            ItemRandomStatsId.Name = "ItemRandomStatsId";
            ItemRandomStatsId.Width = 100;
            // 
            // ItemRequiredAmount
            // 
            ItemRequiredAmount.DataPropertyName = "ItemRequiredAmount";
            ItemRequiredAmount.HeaderText = "Required Amount";
            ItemRequiredAmount.Name = "ItemRequiredAmount";
            ItemRequiredAmount.Width = 100;
            // 
            // ItemImage
            // 
            ItemImage.DataPropertyName = "ItemImage";
            ItemImage.HeaderText = "Image";
            ItemImage.Name = "ItemImage";
            ItemImage.Width = 100;
            // 
            // ItemShape
            // 
            ItemShape.DataPropertyName = "ItemShape";
            ItemShape.HeaderText = "Shape";
            ItemShape.Name = "ItemShape";
            ItemShape.Width = 100;
            // 
            // ItemEffect
            // 
            ItemEffect.DataPropertyName = "ItemEffect";
            ItemEffect.HeaderText = "Effect";
            ItemEffect.Name = "ItemEffect";
            ItemEffect.Width = 100;
            // 
            // ItemStackSize
            // 
            ItemStackSize.DataPropertyName = "ItemStackSize";
            ItemStackSize.HeaderText = "Stack Size";
            ItemStackSize.Name = "ItemStackSize";
            ItemStackSize.Width = 100;
            // 
            // ItemSlots
            // 
            ItemSlots.DataPropertyName = "ItemSlots";
            ItemSlots.HeaderText = "Slots";
            ItemSlots.Name = "ItemSlots";
            ItemSlots.Width = 100;
            // 
            // ItemWeight
            // 
            ItemWeight.DataPropertyName = "ItemWeight";
            ItemWeight.HeaderText = "Weight";
            ItemWeight.Name = "ItemWeight";
            ItemWeight.Width = 100;
            // 
            // ItemLightRange
            // 
            ItemLightRange.DataPropertyName = "ItemLightRange";
            ItemLightRange.HeaderText = "Light Range";
            ItemLightRange.Name = "ItemLightRange";
            ItemLightRange.Width = 100;
            // 
            // ItemLightIntensity
            // 
            ItemLightIntensity.DataPropertyName = "ItemLightIntensity";
            ItemLightIntensity.HeaderText = "Intensity";
            ItemLightIntensity.Name = "ItemLightIntensity";
            ItemLightIntensity.Width = 100;
            // 
            // ItemDurability
            // 
            ItemDurability.DataPropertyName = "ItemDurability";
            ItemDurability.HeaderText = "Durability";
            ItemDurability.Name = "ItemDurability";
            ItemDurability.Width = 100;
            // 
            // ItemPrice
            // 
            ItemPrice.DataPropertyName = "ItemPrice";
            ItemPrice.HeaderText = "Price";
            ItemPrice.Name = "ItemPrice";
            ItemPrice.Width = 100;
            // 
            // ItemToolTip
            // 
            ItemToolTip.DataPropertyName = "ItemToolTip";
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            ItemToolTip.DefaultCellStyle = dataGridViewCellStyle2;
            ItemToolTip.HeaderText = "ToolTip";
            ItemToolTip.Name = "ItemToolTip";
            ItemToolTip.Width = 68;
            // 
            // StartItem
            // 
            StartItem.DataPropertyName = "StartItem";
            StartItem.HeaderText = "Start Item";
            StartItem.Name = "StartItem";
            StartItem.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            StartItem.Width = 100;
            // 
            // NeedIdentify
            // 
            NeedIdentify.DataPropertyName = "NeedIdentify";
            NeedIdentify.HeaderText = "Need Identify";
            NeedIdentify.Name = "NeedIdentify";
            NeedIdentify.Width = 100;
            // 
            // ShowGroupPickup
            // 
            ShowGroupPickup.DataPropertyName = "ShowGroupPickup";
            ShowGroupPickup.HeaderText = "Show Group Pickup";
            ShowGroupPickup.Name = "ShowGroupPickup";
            ShowGroupPickup.Width = 100;
            // 
            // GlobalDropNotify
            // 
            GlobalDropNotify.DataPropertyName = "GlobalDropNotify";
            GlobalDropNotify.HeaderText = "Global Drop Notify";
            GlobalDropNotify.Name = "GlobalDropNotify";
            GlobalDropNotify.Width = 100;
            // 
            // ClassBased
            // 
            ClassBased.DataPropertyName = "ClassBased";
            ClassBased.HeaderText = "Class Based";
            ClassBased.Name = "ClassBased";
            ClassBased.Width = 100;
            // 
            // LevelBased
            // 
            LevelBased.DataPropertyName = "LevelBased";
            LevelBased.HeaderText = "Level Based";
            LevelBased.Name = "LevelBased";
            LevelBased.Width = 100;
            // 
            // CanMine
            // 
            CanMine.DataPropertyName = "CanMine";
            CanMine.HeaderText = "Can Mine";
            CanMine.Name = "CanMine";
            CanMine.Width = 100;
            // 
            // CanFastRun
            // 
            CanFastRun.DataPropertyName = "CanFastRun";
            CanFastRun.HeaderText = "Can FastRun";
            CanFastRun.Name = "CanFastRun";
            CanFastRun.Width = 100;
            // 
            // CanAwakening
            // 
            CanAwakening.DataPropertyName = "CanAwakening";
            CanAwakening.HeaderText = "Can Awakening";
            CanAwakening.Name = "CanAwakening";
            CanAwakening.Width = 100;
            // 
            // panel1
            // 
            panel1.Controls.Add(panel3);
            panel1.Dock = System.Windows.Forms.DockStyle.Top;
            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(1196, 54);
            panel1.TabIndex = 1;
            // 
            // panel3
            // 
            panel3.Controls.Add(Gameshop_button);
            panel3.Controls.Add(btnExport);
            panel3.Controls.Add(btnImport);
            panel3.Controls.Add(lblFilterType);
            panel3.Controls.Add(groupView);
            panel3.Controls.Add(drpFilterType);
            panel3.Controls.Add(lblSearch);
            panel3.Controls.Add(txtSearch);
            panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            panel3.Location = new System.Drawing.Point(0, 0);
            panel3.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            panel3.Name = "panel3";
            panel3.Size = new System.Drawing.Size(1196, 54);
            panel3.TabIndex = 5;
            // 
            // Gameshop_button
            // 
            Gameshop_button.Location = new System.Drawing.Point(903, 25);
            Gameshop_button.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Gameshop_button.Name = "Gameshop_button";
            Gameshop_button.Size = new System.Drawing.Size(97, 27);
            Gameshop_button.TabIndex = 30;
            Gameshop_button.Text = "+ Gameshop";
            Gameshop_button.UseVisualStyleBackColor = true;
            Gameshop_button.Click += Gameshop_button_Click;
            // 
            // btnExport
            // 
            btnExport.Location = new System.Drawing.Point(808, 25);
            btnExport.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btnExport.Name = "btnExport";
            btnExport.Size = new System.Drawing.Size(88, 27);
            btnExport.TabIndex = 6;
            btnExport.Text = "Export";
            btnExport.UseVisualStyleBackColor = true;
            btnExport.Click += btnExport_Click;
            // 
            // btnImport
            // 
            btnImport.Location = new System.Drawing.Point(713, 25);
            btnImport.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btnImport.Name = "btnImport";
            btnImport.Size = new System.Drawing.Size(88, 27);
            btnImport.TabIndex = 5;
            btnImport.Text = "Import";
            btnImport.UseVisualStyleBackColor = true;
            btnImport.Click += btnImport_Click;
            // 
            // lblFilterType
            // 
            lblFilterType.AutoSize = true;
            lblFilterType.Location = new System.Drawing.Point(390, 10);
            lblFilterType.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblFilterType.Name = "lblFilterType";
            lblFilterType.Size = new System.Drawing.Size(43, 17);
            lblFilterType.TabIndex = 3;
            lblFilterType.Text = "Type :";
            // 
            // groupView
            // 
            groupView.Controls.Add(rBtnViewSpecial);
            groupView.Controls.Add(rbtnViewAll);
            groupView.Controls.Add(rbtnViewBinding);
            groupView.Controls.Add(rbtnViewBasic);
            groupView.Controls.Add(rbtnViewStats);
            groupView.Location = new System.Drawing.Point(4, 3);
            groupView.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupView.Name = "groupView";
            groupView.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupView.Size = new System.Drawing.Size(379, 47);
            groupView.TabIndex = 4;
            groupView.TabStop = false;
            groupView.Text = "View Mode";
            // 
            // rBtnViewSpecial
            // 
            rBtnViewSpecial.AutoSize = true;
            rBtnViewSpecial.Location = new System.Drawing.Point(289, 23);
            rBtnViewSpecial.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            rBtnViewSpecial.Name = "rBtnViewSpecial";
            rBtnViewSpecial.Size = new System.Drawing.Size(67, 21);
            rBtnViewSpecial.TabIndex = 4;
            rBtnViewSpecial.TabStop = true;
            rBtnViewSpecial.Text = "Special";
            rBtnViewSpecial.UseVisualStyleBackColor = true;
            rBtnViewSpecial.CheckedChanged += rBtnViewSpecial_CheckedChanged;
            // 
            // rbtnViewAll
            // 
            rbtnViewAll.AutoSize = true;
            rbtnViewAll.Checked = true;
            rbtnViewAll.Location = new System.Drawing.Point(31, 22);
            rbtnViewAll.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            rbtnViewAll.Name = "rbtnViewAll";
            rbtnViewAll.Size = new System.Drawing.Size(40, 21);
            rbtnViewAll.TabIndex = 0;
            rbtnViewAll.TabStop = true;
            rbtnViewAll.Text = "All";
            rbtnViewAll.UseVisualStyleBackColor = true;
            rbtnViewAll.CheckedChanged += rbtnViewAll_CheckedChanged;
            // 
            // rbtnViewBinding
            // 
            rbtnViewBinding.AutoSize = true;
            rbtnViewBinding.Location = new System.Drawing.Point(211, 22);
            rbtnViewBinding.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            rbtnViewBinding.Name = "rbtnViewBinding";
            rbtnViewBinding.Size = new System.Drawing.Size(70, 21);
            rbtnViewBinding.TabIndex = 3;
            rbtnViewBinding.TabStop = true;
            rbtnViewBinding.Text = "Binding";
            rbtnViewBinding.UseVisualStyleBackColor = true;
            rbtnViewBinding.CheckedChanged += rbtnViewBinding_CheckedChanged;
            // 
            // rbtnViewBasic
            // 
            rbtnViewBasic.AutoSize = true;
            rbtnViewBasic.Location = new System.Drawing.Point(80, 22);
            rbtnViewBasic.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            rbtnViewBasic.Name = "rbtnViewBasic";
            rbtnViewBasic.Size = new System.Drawing.Size(56, 21);
            rbtnViewBasic.TabIndex = 1;
            rbtnViewBasic.TabStop = true;
            rbtnViewBasic.Text = "Basic";
            rbtnViewBasic.UseVisualStyleBackColor = true;
            rbtnViewBasic.CheckedChanged += rbtnViewBasic_CheckedChanged;
            // 
            // rbtnViewStats
            // 
            rbtnViewStats.AutoSize = true;
            rbtnViewStats.Location = new System.Drawing.Point(147, 22);
            rbtnViewStats.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            rbtnViewStats.Name = "rbtnViewStats";
            rbtnViewStats.Size = new System.Drawing.Size(54, 21);
            rbtnViewStats.TabIndex = 2;
            rbtnViewStats.TabStop = true;
            rbtnViewStats.Text = "Stats";
            rbtnViewStats.UseVisualStyleBackColor = true;
            rbtnViewStats.CheckedChanged += rbtnViewStats_CheckedChanged;
            // 
            // drpFilterType
            // 
            drpFilterType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            drpFilterType.FormattingEnabled = true;
            drpFilterType.Location = new System.Drawing.Point(393, 28);
            drpFilterType.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            drpFilterType.Name = "drpFilterType";
            drpFilterType.Size = new System.Drawing.Size(140, 25);
            drpFilterType.TabIndex = 2;
            drpFilterType.SelectedIndexChanged += drpFilterType_SelectedIndexChanged;
            // 
            // lblSearch
            // 
            lblSearch.AutoSize = true;
            lblSearch.Location = new System.Drawing.Point(538, 10);
            lblSearch.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblSearch.Name = "lblSearch";
            lblSearch.Size = new System.Drawing.Size(50, 17);
            lblSearch.TabIndex = 1;
            lblSearch.Text = "Search:";
            // 
            // txtSearch
            // 
            txtSearch.Location = new System.Drawing.Point(541, 29);
            txtSearch.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new System.Drawing.Size(164, 23);
            txtSearch.TabIndex = 0;
            txtSearch.KeyDown += txtSearch_KeyDown;
            // 
            // panel2
            // 
            panel2.Controls.Add(itemInfoGridView);
            panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            panel2.Location = new System.Drawing.Point(0, 54);
            panel2.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            panel2.Name = "panel2";
            panel2.Size = new System.Drawing.Size(1196, 500);
            panel2.TabIndex = 2;
            // 
            // ItemInfoFormNew
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1196, 554);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Text = "ItemInfoFormNew";
            FormClosing += ItemInfoFormNew_FormClosing;
            ((System.ComponentModel.ISupportInitialize)itemInfoGridView).EndInit();
            panel1.ResumeLayout(false);
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
            groupView.ResumeLayout(false);
            groupView.PerformLayout();
            panel2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.DataGridView itemInfoGridView;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.GroupBox groupView;
        private System.Windows.Forms.RadioButton rbtnViewAll;
        private System.Windows.Forms.RadioButton rbtnViewBinding;
        private System.Windows.Forms.RadioButton rbtnViewBasic;
        private System.Windows.Forms.RadioButton rbtnViewStats;
        private System.Windows.Forms.RadioButton rBtnViewSpecial;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label lblFilterType;
        private System.Windows.Forms.ComboBox drpFilterType;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnImport;
        private System.Windows.Forms.Button Gameshop_button;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Modified;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemIndex;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemName;
        private System.Windows.Forms.DataGridViewComboBoxColumn ItemType;
        private System.Windows.Forms.DataGridViewComboBoxColumn ItemGrade;
        private System.Windows.Forms.DataGridViewComboBoxColumn ItemRequiredType;
        private System.Windows.Forms.DataGridViewComboBoxColumn ItemRequiredGender;
        private System.Windows.Forms.DataGridViewComboBoxColumn ItemRequiredClass;
        private System.Windows.Forms.DataGridViewComboBoxColumn ItemSet;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemRandomStatsId;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemRequiredAmount;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemImage;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemShape;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemEffect;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemStackSize;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemSlots;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemWeight;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemLightRange;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemLightIntensity;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemDurability;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemPrice;
        private System.Windows.Forms.DataGridViewTextBoxColumn ItemToolTip;
        private System.Windows.Forms.DataGridViewCheckBoxColumn StartItem;
        private System.Windows.Forms.DataGridViewCheckBoxColumn NeedIdentify;
        private System.Windows.Forms.DataGridViewCheckBoxColumn ShowGroupPickup;
        private System.Windows.Forms.DataGridViewCheckBoxColumn GlobalDropNotify;
        private System.Windows.Forms.DataGridViewCheckBoxColumn ClassBased;
        private System.Windows.Forms.DataGridViewCheckBoxColumn LevelBased;
        private System.Windows.Forms.DataGridViewCheckBoxColumn CanMine;
        private System.Windows.Forms.DataGridViewCheckBoxColumn CanFastRun;
        private System.Windows.Forms.DataGridViewCheckBoxColumn CanAwakening;
    }
}