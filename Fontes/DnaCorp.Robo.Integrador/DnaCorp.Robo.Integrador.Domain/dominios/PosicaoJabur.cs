using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnaCorp.Robo.Integrador.Domain.dominios
{
    public class PosicaoJabur
    {
        public Int64 PosicaoId { get; set; }
        public int VeiculoId { get; set; }
        public DateTime DataCadastro { get; set; }
        public DateTime Data { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public int Velocidade { get; set; }
        public string UF { get; set; }
        public string Cidade { get; set; }
        public string Endereco { get; set; }
        public int MacroID { get; set; }
        public string MacroDescricao { get; set; }
        public List<PosicaoJaburEvento> Eventos { get; set; }

    }

    public class PosicaoJaburEvento
    {
        public Int64 PosicaoId { get; set; }
        public string EventoCodigo { get; set; }
        public string EventoValor { get; set; }
        public int EventoId { get; set; }
    }
}
