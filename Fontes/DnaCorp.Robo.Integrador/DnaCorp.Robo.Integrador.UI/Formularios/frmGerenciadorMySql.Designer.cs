namespace DnaCorp.Robo.Integrador.UI.Formularios
{
    partial class frmGerenciadorMySql
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
            this.txtProvider = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtComando = new System.Windows.Forms.TextBox();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.btnTesteConexao = new System.Windows.Forms.Button();
            this.btnExecutar = new System.Windows.Forms.Button();
            this.txtErro = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // txtProvider
            // 
            this.txtProvider.Location = new System.Drawing.Point(45, 62);
            this.txtProvider.Name = "txtProvider";
            this.txtProvider.Size = new System.Drawing.Size(685, 20);
            this.txtProvider.TabIndex = 0;
            this.txtProvider.Text = "Server=10.10.100.15;User ID=interage;Password=SIGhRA@20;Database=mclient";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(42, 37);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Provider";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(42, 102);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(52, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Comando";
            // 
            // txtComando
            // 
            this.txtComando.Location = new System.Drawing.Point(45, 127);
            this.txtComando.Multiline = true;
            this.txtComando.Name = "txtComando";
            this.txtComando.Size = new System.Drawing.Size(371, 67);
            this.txtComando.TabIndex = 2;
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(45, 219);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(825, 264);
            this.dataGridView1.TabIndex = 4;
            // 
            // btnTesteConexao
            // 
            this.btnTesteConexao.AutoSize = true;
            this.btnTesteConexao.Location = new System.Drawing.Point(302, 92);
            this.btnTesteConexao.Name = "btnTesteConexao";
            this.btnTesteConexao.Size = new System.Drawing.Size(89, 23);
            this.btnTesteConexao.TabIndex = 5;
            this.btnTesteConexao.Text = "Teste Conexao";
            this.btnTesteConexao.UseVisualStyleBackColor = true;
            this.btnTesteConexao.Click += new System.EventHandler(this.btnTesteConexao_Click);
            // 
            // btnExecutar
            // 
            this.btnExecutar.AutoSize = true;
            this.btnExecutar.Location = new System.Drawing.Point(415, 92);
            this.btnExecutar.Name = "btnExecutar";
            this.btnExecutar.Size = new System.Drawing.Size(103, 23);
            this.btnExecutar.TabIndex = 6;
            this.btnExecutar.Text = "Executa comando";
            this.btnExecutar.UseVisualStyleBackColor = true;
            this.btnExecutar.Click += new System.EventHandler(this.btnExecutar_Click);
            // 
            // txtErro
            // 
            this.txtErro.Location = new System.Drawing.Point(443, 127);
            this.txtErro.Multiline = true;
            this.txtErro.Name = "txtErro";
            this.txtErro.Size = new System.Drawing.Size(371, 67);
            this.txtErro.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(762, 102);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(52, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Comando";
            // 
            // frmGerenciadorMySql
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(894, 495);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtErro);
            this.Controls.Add(this.btnExecutar);
            this.Controls.Add(this.btnTesteConexao);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtComando);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtProvider);
            this.Name = "frmGerenciadorMySql";
            this.Text = "v";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtProvider;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtComando;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button btnTesteConexao;
        private System.Windows.Forms.Button btnExecutar;
        private System.Windows.Forms.TextBox txtErro;
        private System.Windows.Forms.Label label3;
    }
}