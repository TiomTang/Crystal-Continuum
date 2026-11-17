namespace Server
{
    partial class ExpConfigForm
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
            this.expTabs = new System.Windows.Forms.TabControl();
            this.playerTab = new System.Windows.Forms.TabPage();
            this.playerGridView = new System.Windows.Forms.DataGridView();
            this.playerLevelColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.playerExperienceColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.heroTab = new System.Windows.Forms.TabPage();
            this.heroGridView = new System.Windows.Forms.DataGridView();
            this.heroLevelColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.heroExperienceColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.buttonPanel = new System.Windows.Forms.Panel();
            this.btnAddLevel = new System.Windows.Forms.Button();
            this.btnRemoveLevel = new System.Windows.Forms.Button();
            this.lblLevelCaption = new System.Windows.Forms.Label();
            this.lblLevelCount = new System.Windows.Forms.Label();
            this.expTabs.SuspendLayout();
            this.playerTab.SuspendLayout();
            this.heroTab.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.playerGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.heroGridView)).BeginInit();
            // 
            // expTabs
            // 
            this.expTabs.Controls.Add(this.playerTab);
            this.expTabs.Controls.Add(this.heroTab);
            this.expTabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.expTabs.Location = new System.Drawing.Point(0, 0);
            this.expTabs.Name = "expTabs";
            this.expTabs.SelectedIndex = 0;
            this.expTabs.Size = new System.Drawing.Size(560, 580);
            this.expTabs.TabIndex = 0;
            this.expTabs.SelectedIndexChanged += new System.EventHandler(this.expTabs_SelectedIndexChanged);
            // 
            // playerTab
            // 
            this.playerTab.Controls.Add(this.playerGridView);
            this.playerTab.Location = new System.Drawing.Point(4, 22);
            this.playerTab.Name = "playerTab";
            this.playerTab.Padding = new System.Windows.Forms.Padding(3);
            this.playerTab.Size = new System.Drawing.Size(552, 554);
            this.playerTab.TabIndex = 0;
            this.playerTab.Text = "Player";
            this.playerTab.UseVisualStyleBackColor = true;
            // 
            // playerGridView
            // 
            this.playerGridView.AllowUserToAddRows = false;
            this.playerGridView.AllowUserToDeleteRows = false;
            this.playerGridView.BackgroundColor = System.Drawing.SystemColors.Window;
            this.playerGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.playerGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.playerLevelColumn,
            this.playerExperienceColumn});
            this.playerGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.playerGridView.Location = new System.Drawing.Point(3, 3);
            this.playerGridView.Name = "playerGridView";
            this.playerGridView.RowHeadersVisible = false;
            this.playerGridView.Size = new System.Drawing.Size(546, 548);
            this.playerGridView.TabIndex = 0;
            this.playerGridView.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.ExpGridView_CellValidating);
            this.playerGridView.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.ExpGridView_CellValueChanged);
            // 
            // playerLevelColumn
            // 
            this.playerLevelColumn.HeaderText = "Level";
            this.playerLevelColumn.Name = "Level";
            this.playerLevelColumn.ReadOnly = true;
            this.playerLevelColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.playerLevelColumn.Width = 70;
            // 
            // playerExperienceColumn
            // 
            this.playerExperienceColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.playerExperienceColumn.HeaderText = "Experience required";
            this.playerExperienceColumn.Name = "Experience";
            this.playerExperienceColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.playerExperienceColumn.ValueType = typeof(long);
            // 
            // heroTab
            // 
            this.heroTab.Controls.Add(this.heroGridView);
            this.heroTab.Location = new System.Drawing.Point(4, 22);
            this.heroTab.Name = "heroTab";
            this.heroTab.Padding = new System.Windows.Forms.Padding(3);
            this.heroTab.Size = new System.Drawing.Size(552, 554);
            this.heroTab.TabIndex = 1;
            this.heroTab.Text = "Hero";
            this.heroTab.UseVisualStyleBackColor = true;
            // 
            // heroGridView
            // 
            this.heroGridView.AllowUserToAddRows = false;
            this.heroGridView.AllowUserToDeleteRows = false;
            this.heroGridView.BackgroundColor = System.Drawing.SystemColors.Window;
            this.heroGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.heroGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.heroLevelColumn,
            this.heroExperienceColumn});
            this.heroGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.heroGridView.Location = new System.Drawing.Point(3, 3);
            this.heroGridView.Name = "heroGridView";
            this.heroGridView.RowHeadersVisible = false;
            this.heroGridView.Size = new System.Drawing.Size(546, 548);
            this.heroGridView.TabIndex = 0;
            this.heroGridView.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.ExpGridView_CellValidating);
            this.heroGridView.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.ExpGridView_CellValueChanged);
            // 
            // heroLevelColumn
            // 
            this.heroLevelColumn.HeaderText = "Level";
            this.heroLevelColumn.Name = "Level";
            this.heroLevelColumn.ReadOnly = true;
            this.heroLevelColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.heroLevelColumn.Width = 70;
            // 
            // heroExperienceColumn
            // 
            this.heroExperienceColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.heroExperienceColumn.HeaderText = "Experience required";
            this.heroExperienceColumn.Name = "Experience";
            this.heroExperienceColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.heroExperienceColumn.ValueType = typeof(long);
            // 
            // buttonPanel
            // 
            this.buttonPanel.Controls.Add(this.btnAddLevel);
            this.buttonPanel.Controls.Add(this.btnRemoveLevel);
            this.buttonPanel.Controls.Add(this.lblLevelCaption);
            this.buttonPanel.Controls.Add(this.lblLevelCount);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPanel.Location = new System.Drawing.Point(0, 580);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Size = new System.Drawing.Size(560, 40);
            this.buttonPanel.TabIndex = 1;
            // 
            // btnAddLevel
            // 
            this.btnAddLevel.Location = new System.Drawing.Point(8, 8);
            this.btnAddLevel.Name = "btnAddLevel";
            this.btnAddLevel.Size = new System.Drawing.Size(110, 25);
            this.btnAddLevel.TabIndex = 0;
            this.btnAddLevel.Text = "Add Level";
            this.btnAddLevel.UseVisualStyleBackColor = true;
            this.btnAddLevel.Click += new System.EventHandler(this.btnAddLevel_Click);
            // 
            // btnRemoveLevel
            // 
            this.btnRemoveLevel.Location = new System.Drawing.Point(126, 8);
            this.btnRemoveLevel.Name = "btnRemoveLevel";
            this.btnRemoveLevel.Size = new System.Drawing.Size(150, 25);
            this.btnRemoveLevel.TabIndex = 1;
            this.btnRemoveLevel.Text = "Remove Last Level";
            this.btnRemoveLevel.UseVisualStyleBackColor = true;
            this.btnRemoveLevel.Click += new System.EventHandler(this.btnRemoveLevel_Click);
            // 
            // lblLevelCaption
            // 
            this.lblLevelCaption.AutoSize = true;
            this.lblLevelCaption.Location = new System.Drawing.Point(300, 13);
            this.lblLevelCaption.Name = "lblLevelCaption";
            this.lblLevelCaption.Size = new System.Drawing.Size(46, 13);
            this.lblLevelCaption.TabIndex = 2;
            this.lblLevelCaption.Text = "Levels:";
            // 
            // lblLevelCount
            // 
            this.lblLevelCount.AutoSize = true;
            this.lblLevelCount.Location = new System.Drawing.Point(352, 13);
            this.lblLevelCount.Name = "lblLevelCount";
            this.lblLevelCount.Size = new System.Drawing.Size(13, 13);
            this.lblLevelCount.TabIndex = 3;
            this.lblLevelCount.Text = "0";
            // 
            // ExpConfigForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(560, 620);
            this.Controls.Add(this.expTabs);
            this.Controls.Add(this.buttonPanel);
            this.Name = "ExpConfigForm";
            this.Text = "Level Experience";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ExpConfigForm_FormClosed);
            this.expTabs.ResumeLayout(false);
            this.playerTab.ResumeLayout(false);
            this.heroTab.ResumeLayout(false);
            this.buttonPanel.ResumeLayout(false);
            this.buttonPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.playerGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.heroGridView)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl expTabs;
        private System.Windows.Forms.TabPage playerTab;
        private System.Windows.Forms.TabPage heroTab;
        private System.Windows.Forms.DataGridView playerGridView;
        private System.Windows.Forms.DataGridView heroGridView;
        private System.Windows.Forms.DataGridViewTextBoxColumn playerLevelColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn playerExperienceColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn heroLevelColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn heroExperienceColumn;
        private System.Windows.Forms.Panel buttonPanel;
        private System.Windows.Forms.Button btnAddLevel;
        private System.Windows.Forms.Button btnRemoveLevel;
        private System.Windows.Forms.Label lblLevelCaption;
        private System.Windows.Forms.Label lblLevelCount;
    }
}
