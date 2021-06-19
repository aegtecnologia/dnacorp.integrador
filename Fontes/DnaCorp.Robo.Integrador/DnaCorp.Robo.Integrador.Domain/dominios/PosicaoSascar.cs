using System;
using System.Collections.Generic;
using System.Linq;

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
        public List<PosicaoSascarEvento> Eventos { get; set; }

        public PosicaoSascar(List<int> eventos, HashSet<SascarCadEvento> eventos_cad)
        {
            Eventos = new List<PosicaoSascarEvento>();
            foreach (var evento in eventos)
            {
                this.Eventos.Add(new PosicaoSascarEvento(evento, eventos_cad));
            }
        }
    }

    public class PosicaoSascarEvento
    {
        public int Codigo { get; set; }
        public int StatusCodigo { get; set; }
        public string StatusDescricao { get; set; }
        public string EventoDescricao { get; set; }

        public PosicaoSascarEvento(int codigo, HashSet<SascarCadEvento> eventos_cad)
        {
            if (codigo > 0)
            {

                Codigo = codigo;
                StatusCodigo = 1;
                StatusDescricao = "Ativado";
            }
            else
            {
                Codigo = codigo * (-1);
                StatusCodigo = 0;
                StatusDescricao = "Destativado";
            }

            try
            {
                EventoDescricao = eventos_cad.Where(e => e.atuador == Codigo).FirstOrDefault().descricao;
            }
            catch
            {
                EventoDescricao = "";
            }


        }
    }

    public class SascarCadEvento
    {
        public int idSequenciamentoEvento { get; set; }
        public int atuador { get; set; }
        public string descricao { get; set; }

    }
}
