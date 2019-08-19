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

            var servico = new ObterVeiculosJaburJobService();
            var frm = new frmIntegrador(servico, intervalo);
            frm.Text = "Integração de veiculos - JABUR";
            frm.Show();
        }

        private void mnuObterVeiculosJabur_Click(object sender, EventArgs e)
        {
            dynamic config = ConfigurationHelper.getConfiguration();
            int intervalo = Convert.ToInt32(config.Rastreadores.Jabur.ObterVeiculos.Intervalo);

            var servico = new ObterPosicoesJaburJobService();
            var frm = new frmIntegrador(servico, intervalo);
            frm.Text = "Integração de posições - JABUR";
            frm.Show();
        }

        private void frmHome_Load(object sender, EventArgs e)
        {

        }

        private void mnuObterVeiculosSascar_Click(object sender, EventArgs e)
        {
            dynamic config = ConfigurationHelper.getConfiguration();
            int intervalo = Convert.ToInt32(config.Rastreadores.Sascar.ObterVeiculos.Intervalo);

            var servico = new ObterPosicoesSascarJobService();
            var frm = new frmIntegrador(servico, intervalo);
            frm.Text = "Integração de posições - SASCAR";
            frm.Show();
        }

        private void mnuObterPosicoesSascar_Click(object sender, EventArgs e)
        {
            dynamic config = ConfigurationHelper.getConfiguration();
            int intervalo = Convert.ToInt32(config.Rastreadores.Sascar.ObterPosicoes.Intervalo);

            var servico = new ObterVeiculosSascarJobService();
            var frm = new frmIntegrador(servico, intervalo);
            frm.Text = "Integração de veiculos - SASCAR";
            frm.Show();
        }
    }
}
