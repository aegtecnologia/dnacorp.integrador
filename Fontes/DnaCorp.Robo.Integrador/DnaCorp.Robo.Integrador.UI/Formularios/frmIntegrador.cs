using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DnaCorp.Robo.Integrador.Service.JOB;

namespace DnaCorp.Robo.Integrador.UI.Formularios
{
    public partial class frmIntegrador : Form
    {
        private delegate void AtualizaLogCallBack(string strMensagem);
        private IObterDados _job { get; set; }
        private List<ObterDadosResponse> lista { get; set; }
        private int Intervalo { get; set; }

        public frmIntegrador(IObterDados job, int intervalo)
        {
            InitializeComponent();
            timer1.Interval = intervalo;

            _job = job;
            lista = new List<ObterDadosResponse>();
        }

        private void AtualizaLog(string strMensagem)
        {
            // Anexa texto ao final de cada linha
            txtTecnologia.Text = strMensagem;
            Application.DoEvents();
        }

        //private void ExecutarMock()
        //{
        //    var response = new ObterDadosResponse();
        //    response.DataInicial = DateTime.Now;
        //    Thread.Sleep(10000);
        //    response.TotalRegistros = 100;
        //    response.DataFinal = DateTime.Now;
        //    response.Mensagem = "Executado com sucesso";

        //    lista.Add(response);

        //    var query = lista.OrderByDescending(e => e.DataInicial);

        //    var texto = new StringBuilder();
        //    texto.AppendLine("Data inicial | Data final | Total de Registros");

        //    foreach (var item in query)
        //    {
        //        texto.AppendLine($"Iniciado em:{item.DataInicial.ToString("HH:mm:ss")} - Finalizado em: {item.DataFinal.ToString("HH: mm:ss")} - Total de registros:{item.TotalRegistros}");
        //    }

        //    txtTecnologia.Text = texto.ToString();
        //}
        private void Executar()
        {
            this.Invoke(new AtualizaLogCallBack(this.AtualizaLog), new object[] { "Aguarde ..." });
            var response = _job.Executa();

            lista.Add(response);

            var query = lista.OrderByDescending(e => e.DataInicial);

            var texto = new StringBuilder();
            //texto.AppendLine("Data inicial | Data final | Total de Registros");
            texto.AppendLine($"Próxima execução as {query.SingleOrDefault().DataFinal.AddMilliseconds(timer1.Interval).ToString("HH:mm:ss")}");


            foreach (var item in query)
            {
                texto.AppendLine($"Iniciado em:{item.DataInicial.ToString("HH:mm:ss")} - Finalizado em: {item.DataFinal.ToString("HH: mm:ss")} - Total de registros:{item.TotalRegistros} - Msg: {item.Mensagem}");
            }

            this.Invoke(new AtualizaLogCallBack(this.AtualizaLog), new object[] { texto.ToString() });

       //     txtTecnologia.Text = texto.ToString();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                Executar();
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void frmIntegrador_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                timer1.Stop();
                timer1.Dispose();
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void frmIntegrador_Load(object sender, EventArgs e)
        {
            timer1.Enabled = true;
            timer1.Start();

            txtTecnologia.Text = $"Próxima execução as {DateTime.Now.AddMilliseconds(timer1.Interval).ToString("HH:mm:ss")}";
        }

        private void mnuExecutar_Click(object sender, EventArgs e)
        {
            Thread tr = new Thread(Executar);
            tr.Start();
        }

        private void mnuSair_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
