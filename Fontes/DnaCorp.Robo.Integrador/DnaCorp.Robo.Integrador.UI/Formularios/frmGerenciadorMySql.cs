using DnaCorp.Robo.Integrador.Infra.MySql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DnaCorp.Robo.Integrador.UI.Formularios
{
    public partial class frmGerenciadorMySql : Form
    {
        public frmGerenciadorMySql()
        {
            InitializeComponent();
        }

        private void btnTesteConexao_Click(object sender, EventArgs e)
        {
            try
            {
                var conexao = new ConexaoMySql();
                var dt = conexao.RetornaDT(txtComando.Text);
                dataGridView1.DataSource = dt;
                this.Text = $"Total de registros {dt.Rows.Count}";
            }
            catch (Exception erro)
            {
                MessageBox.Show($"Ocorreu um erro: {erro.Message}");
                dataGridView1.DataSource = "";
                this.Text = $"";
            }
        }

        private void btnExecutar_Click(object sender, EventArgs e)
        {
            try
            {
                var conexao = new ConexaoMySql();
                var dt = conexao.RetornaDT(txtComando.Text);
                dataGridView1.DataSource = dt;
            }
            catch (Exception erro)
            {
                txtErro.Text = $"Ocorreu um erro: {erro.Message}";
            }
        }
    }
}
