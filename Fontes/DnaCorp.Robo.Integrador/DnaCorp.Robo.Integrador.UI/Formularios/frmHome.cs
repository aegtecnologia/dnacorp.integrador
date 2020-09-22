using DnaCorp.Robo.Integrador.Service.Helper;
using DnaCorp.Robo.Integrador.Service.JOB;
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
    public partial class frmHome : Form
    {
        public frmHome()
        {
            InitializeComponent();
        }

        private void mnuObterPosicoesJabur_Click(object sender, EventArgs e)
        {
            dynamic config = ConfigurationHelper.getConfiguration();
            int intervalo = Convert.ToInt32(config.Rastreadores.Jabur.ObterPosicoes.Intervalo);

            var servico = new ObterPosicoesJaburJobService();
            var frm = new frmIntegrador(servico, intervalo);
            frm.Text = "Integração de posições - JABUR";
            frm.Show();
        }

        private void mnuObterVeiculosJabur_Click(object sender, EventArgs e)
        {
            dynamic config = ConfigurationHelper.getConfiguration();
            int intervalo = Convert.ToInt32(config.Rastreadores.Jabur.ObterVeiculos.Intervalo);

            var servico = new ObterVeiculosJaburJobService();
            var frm = new frmIntegrador(servico, intervalo);
            frm.Text = "Integração de veiculos - JABUR";
            frm.Show();
        }

        private void frmHome_Load(object sender, EventArgs e)
        {
            

        }

        private void mnuObterVeiculosSascar_Click(object sender, EventArgs e)
        {
            dynamic config = ConfigurationHelper.getConfiguration();
            int intervalo = Convert.ToInt32(config.Rastreadores.Sascar.ObterVeiculos.Intervalo);

            var servico = new ObterVeiculosSascarJobService();
            var frm = new frmIntegrador(servico, intervalo);
            frm.Text = "Integração de veiculos - SASCAR";
            frm.Show();
        }

        private void mnuObterPosicoesSascar_Click(object sender, EventArgs e)
        {
            dynamic config = ConfigurationHelper.getConfiguration();
            int intervalo = Convert.ToInt32(config.Rastreadores.Sascar.ObterPosicoes.Intervalo);

            var servico = new ObterPosicoesSascarJobService();
            var frm = new frmIntegrador(servico, intervalo);
            frm.Text = "Integração de posições - SASCAR";
            frm.Show();
        }

        private void MnuObterVeiculosAutotrac_Click(object sender, EventArgs e)
        {
            dynamic config = ConfigurationHelper.getConfiguration();
            int intervalo = Convert.ToInt32(config.Rastreadores.Autotrac.ObterVeiculos.Intervalo);

            var servico = new ObterVeiculosAutotracJobService();
            var frm = new frmIntegrador(servico, intervalo);
            frm.Text = "Integração de veiculos - AUTOTRAC";
            frm.Show();
        }

        private void MnuObterPosicoesAutotrac_Click(object sender, EventArgs e)
        {
            dynamic config = ConfigurationHelper.getConfiguration();
            int intervalo = Convert.ToInt32(config.Rastreadores.Autotrac.ObterPosicoes.Intervalo);

            var servico = new ObterPosicoesAutotracJobService();
            var frm = new frmIntegrador(servico, intervalo);
            frm.Text = "Integração de posições - AUTOTRAC";
            frm.Show();
        }

        private void mnuObterPosicoesOmnilink_Click(object sender, EventArgs e)
        {
            dynamic config = ConfigurationHelper.getConfiguration();
            int intervalo = Convert.ToInt32(config.Rastreadores.Omnilink.ObterPosicoes.Intervalo);

            var servico = new ObterPosicoesOmnilinkJobService();
            var frm = new frmIntegrador(servico, intervalo);
            frm.Text = "Integração de posições - OMNILINK";
            frm.Show();
        }

        private void mnuTesteMysql_Click(object sender, EventArgs e)
        {
            var f = new frmGerenciadorMySql();
            f.Show();
        }

        private void obterPosicoesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dynamic config = ConfigurationHelper.getConfiguration();
            int intervalo = Convert.ToInt32(config.Rastreadores.Sighra.ObterPosicoes.Intervalo);

            var servico = new ObterPosicoesSighraJobService();
            var frm = new frmIntegrador(servico, intervalo);
            frm.Text = "Integração de posições - Sighra";
            frm.Show();
        }

        private void testeIntegracaoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var f = new frmTesteIntegracao();
            f.Show();
        }

        private void obterPosicoesToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            dynamic config = ConfigurationHelper.getConfiguration();
            int intervalo = Convert.ToInt32(config.Rastreadores.SitaCom.ObterPosicoes.Intervalo);

            var servico = new ObterPosicoesSitaComJobService();
            var frm = new frmIntegrador(servico, intervalo);
            frm.Text = "Integração de posições - SITACOM";
            frm.Show();
        }
    }
}
