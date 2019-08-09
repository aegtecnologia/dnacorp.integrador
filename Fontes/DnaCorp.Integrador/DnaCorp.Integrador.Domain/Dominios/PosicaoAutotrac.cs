using System;
using System.Collections.Generic;
using System.Text;

namespace DnaCorp.Integrador.Domain.Dominios
{
    public class PosicaoAutotrac
    {
        public Int64 PosicaoId { get; set; }
        public int VeiculoEnd { get; set; }
        public DateTime DataCadastro { get; set; }
        public DateTime Data { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string UF { get; set; }
        public string Cidade { get; set; }
        public string Endereco { get; set; }
    }
}
