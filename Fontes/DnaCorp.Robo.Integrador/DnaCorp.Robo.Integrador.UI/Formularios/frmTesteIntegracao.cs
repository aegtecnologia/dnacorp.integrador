using DnaCorp.Robo.Integrador.Domain.dominios;
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
    public partial class frmTesteIntegracao : Form
    {
        public frmTesteIntegracao()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //var service = new ObterVeiculosJaburJobService();
            //var res = service.ObterMacros();

            var service = new ObterPosicoesJaburJobService();
            List<PosicaoJabur> res = service.ObterPosicoes();

            textBox1.Text = res.Count.ToString();
        }
    }
}
