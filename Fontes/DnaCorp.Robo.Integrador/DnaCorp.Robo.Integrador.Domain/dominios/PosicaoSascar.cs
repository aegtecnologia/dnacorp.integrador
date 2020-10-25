using System;

namespace DnaCorp.Robo.Integrador.Domain.dominios
{
    public class PosicaoSascar
    {
        public Int64 PosicaoId { get; set; }
        public int VeiculoId { get; set; }
        public DateTime DataCadastro { get; set; }
        public DateTime Data { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string UF { get; set; }
        public string Cidade { get; set; }
        public string Endereco { get; set; }
        public int Velocidade { get; set; }
        public int EventoId { get; set; }
        public string EventoDescricao { get; set; }
    }
}
