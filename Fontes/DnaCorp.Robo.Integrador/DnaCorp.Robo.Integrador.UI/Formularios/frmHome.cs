using DnaCorp.Robo.Integrador.Service.Helper;
using DnaCorp.Robo.Integrador.Service.JOB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DnaCorp.Robo.Integrador.UI.Formularios
{
    public class Carro
    {
        public int CarroId { get; set; }
        public int Portas { get; set; }
        public int Cavalos { get; set; }
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public string evt1 { get; set; }
        public string evt2 { get; set; }
        public string evt3 { get; set; }
        public string evt4 { get; set; }

    }

    public class Evento
    {
        public string codigo { get; set; }
        public string valor { get; set; }
    }

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
            var carro = new
            {
                CarroId = 1,
                Cavalos = 8,
                Marca = "Ford",
                Modelo = "Fiesta",
                Portas = 4,
                evt1 = true,
                evt2 = 1,
                evt3 = -1,
                evt4 = "2",


            };


            var listaEventos = new List<Evento>();

            //foreach (PropertyInfo propertyInfo in carro.GetType().GetProperties())
            foreach (PropertyInfo propertyInfo in carro.GetType().GetProperties())
            {
                if (propertyInfo.Name.Contains("evt"))
                {
                    listaEventos.Add(new Evento()
                    {
                        codigo = propertyInfo.Name,
                        valor = propertyInfo.GetValue(carro).ToString()

                    });
                }
                    
            }
            
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
            DateTime agora = DateTime.Now;
            DateTime ultimaData = new DateTime(2020, 12, 19);
            var dif = agora.Subtract(ultimaData);

            
            //dynamic config = ConfigurationHelper.getConfiguration();
            //int intervalo = Convert.ToInt32(config.Rastreadores.Autotrac.ObterVeiculos.Intervalo);

            //var servico = new ObterVeiculosAutotracJobService();
            //var frm = new frmIntegrador(servico, intervalo);
            //frm.Text = "Integração de veiculos - AUTOTRAC";
            //frm.Show();
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
