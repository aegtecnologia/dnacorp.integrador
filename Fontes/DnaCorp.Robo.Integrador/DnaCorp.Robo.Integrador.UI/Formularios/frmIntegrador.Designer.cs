namespace DnaCorp.Robo.Integrador.UI.Formularios
{
    partial class frmIntegrador
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
            this.components = new System.ComponentModel.Container();
            this.txtTecnologia = new System.Windows.Forms.TextBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.comandosToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuExecutar = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuSair = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtTecnologia
            // 
            this.txtTecnologia.BackColor = System.Drawing.Color.Black;
            this.txtTecnologia.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtTecnologia.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtTecnologia.ForeColor = System.Drawing.Color.Green;
            this.txtTecnologia.Location = new System.Drawing.Point(0, 24);
            this.txtTecnologia.Multiline = true;
            this.txtTecnologia.Name = "txtTecnologia";
            this.txtTecnologia.ReadOnly = true;
            this.txtTecnologia.Size = new System.Drawing.Size(1008, 437);
            this.txtTecnologia.TabIndex = 0;
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.comandosToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1008, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // comandosToolStripMenuItem
            // 
            this.comandosToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuExecutar,
            this.mnuSair});
            this.comandosToolStripMenuItem.Name = "comandosToolStripMenuItem";
            this.comandosToolStripMenuItem.Size = new System.Drawing.Size(77, 20);
            this.comandosToolStripMenuItem.Text = "Comandos";
            // 
            // mnuExecutar
            // 
            this.mnuExecutar.Name = "mnuExecutar";
            this.mnuExecutar.Size = new System.Drawing.Size(180, 22);
            this.mnuExecutar.Text = "Executar";
            this.mnuExecutar.Click += new System.EventHandler(this.mnuExecutar_Click);
            // 
            // mnuSair
            // 
            this.mnuSair.Name = "mnuSair";
            this.mnuSair.Size = new System.Drawing.Size(180, 22);
            this.mnuSair.Text = "Sair";
            this.mnuSair.Click += new System.EventHandler(this.mnuSair_Click);
            // 
            // frmIntegrador
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Gray;
            this.ClientSize = new System.Drawing.Size(1008, 461);
            this.Controls.Add(this.txtTecnologia);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "frmIntegrador";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "frmIntegrador";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmIntegrador_FormClosing);
            this.Load += new System.EventHandler(this.frmIntegrador_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtTecnologia;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem comandosToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mnuExecutar;
        private System.Windows.Forms.ToolStripMenuItem mnuSair;
    }
}