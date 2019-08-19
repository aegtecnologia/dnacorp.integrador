using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnaCorp.Robo.Integrador.Service.JOB
{
    public class ObterDadosResponse
    {
        public int TotalRegistros { get; set; }
        public DateTime DataInicial { get; set; }
        public DateTime DataFinal { get; set; }
        public string Mensagem { get; set; }
    }
}
