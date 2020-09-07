using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnaCorp.Robo.Integrador.Domain.dominios
{
    public class PosicaoAutotrac
    {
        public Int64 PosicaoId { get; set; }
        public Int64 VeiculoId { get; set; }
        public DateTime DataCadastro { get; set; }
        public DateTime DataPosicao { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string UF { get; set; }
        public string Endereco { get; set; }
        public string Referencia { get; set; }
    }
}
