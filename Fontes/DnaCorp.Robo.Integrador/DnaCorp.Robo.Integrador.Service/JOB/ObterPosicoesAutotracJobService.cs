using DnaCorp.Robo.Integrador.Domain.dominios;
using DnaCorp.Robo.Integrador.Infra.MSSQL;
using DnaCorp.Robo.Integrador.Service.Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DnaCorp.Robo.Integrador.Service.JOB
{
    public class ObterPosicoesAutotracJobService : IObterDados
    {
        private string Endereco { get; set; }
        private string Usuario { get; set; }
        private string Senha { get; set; }
        private string ContaEmpresa { get; set; }
        private string Chave { get; set; }
        private bool Ativo { get; set; }

        private Conexao _conexao;

        public ObterPosicoesAutotracJobService()
        {
            _conexao = new Conexao();

            dynamic config = ConfigurationHelper.getConfiguration();
            var provider = Convert.ToString(config.ConnectionStrings.DefaultConnection);
            Endereco = Convert.ToString(config.Rastreadores.Autotrac.Endereco);
            Usuario = Convert.ToString(config.Rastreadores.Autotrac.Usuario);
            Senha = Convert.ToString(config.Rastreadores.Autotrac.Senha);
            ContaEmpresa = Convert.ToString(config.Rastreadores.Autotrac.Conta);
            Chave = Convert.ToString(config.Rastreadores.Autotrac.Chave);
            Ativo = Convert.ToBoolean(config.Rastreadores.Autotrac.ObterPosicoes.Ativo);

            _conexao.Configura(provider);
        }
        public ObterDadosResponse Executa()
        {
            var response = new ObterDadosResponse();

            try
            {
                response.DataInicial = DateTime.Now;

                if (!Ativo) throw new Exception("Job inativo");

                var posicoes = ObterPosicoes();

                PersistirDados(posicoes);

                Criar_Log($"Processado com sucesso", true);
                response.TotalRegistros = posicoes.Count;
                response.Mensagem = "Processado com sucesso!";
            }
            catch (Exception erro)
            {
                Criar_Log(erro.Message, false);
                response.Mensagem = erro.Message;
            }
            finally
            {
                response.DataFinal = DateTime.Now;
            }

            return response;
        }

        private void Criar_Log(string mensagem, bool sucesso)
        {
            try
            {
                var comando = $@"INSERT INTO LOG_AUTOMACAO VALUES(
GETDATE(),
'{nameof(ObterPosicoesAutotracJobService)}',
{(sucesso ? 1 : 0).ToString()},
'{mensagem}'
)";
                _conexao.Executa(comando);

            }
            catch (Exception erro)
            {
                mensagem = erro.Message;
            }
            finally
            {
                LogHelper.CriarLog($"{nameof(ObterPosicoesAutotracJobService)} - {mensagem}", sucesso);
            }
        }

        private void ObterPosicaoPorVeiculo(int veiculoId, ref List<PosicaoAutotrac> posicoes)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(Endereco);


            //client.DefaultRequestHeaders.Add("Content-Type", "application/json");
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Chave);
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {Usuario}:{Senha}");

            var request = $"accounts/{ContaEmpresa}/vehicles/{veiculoId}/positions";
            HttpResponseMessage response = client.GetAsync(request).Result;

            if (!response.IsSuccessStatusCode) throw new Exception($"Falha na requisição de posições");

            var jsonString = response.Content.ReadAsStringAsync().Result;
            var dataResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<GetPosicoesResponse>(jsonString);
            var p = dataResponse.Data.Last();

            posicoes.Add(new PosicaoAutotrac()
            {
                VeiculoEnd = p.VehicleAddress,
                Data = p.PositionTime,
                DataCadastro = DateTime.Now,
                Endereco = p.Landmark,
                Latitude = p.Latitude.ToString(),
                Longitude = p.Longitude.ToString(),
                Cidade = "",
                UF = "",
            });

        }

        private List<PosicaoAutotrac> ObterPosicoes()
        {
            var posicoes = new List<PosicaoAutotrac>();
            var veiculos = ObterListaDeVeiculos();
            foreach (var veiculoId in veiculos)
                ObterPosicaoPorVeiculo(veiculoId, ref posicoes);

            return posicoes;
        }

        private int[] ObterListaDeVeiculosMock()
        {
            return new int[] { 151 };
        }

        private List<int> ObterListaDeVeiculos()
        {
            var lista = new List<int>();
            var tabela = _conexao.RetornaDT("select * from veiculo_autotrac");
            foreach (DataRow dr in tabela.Rows)
                lista.Add(Convert.ToInt32(dr["veiculoId"]));

            return lista;
        }

        private void PersistirDados(List<PosicaoAutotrac> posicoes)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var p in posicoes)
            {
                sb.AppendLine($@"insert into posicoes_autotrac values (
{p.VeiculoEnd.ToString()},
getdate(),
'{p.Data.ToString("yyyy/MM/dd HH:mm:ss")}',
'{p.Latitude}',
'{p.Longitude}',
'{p.UF}',
'{p.Cidade}',
'{p.Endereco}');");
            }

            _conexao.Executa(sb.ToString());
        }

        internal class GetPosicoesItemResponse
        {
            public int ID { get; set; }
            public int AccountNumber { get; set; }
            public int VehicleAddress { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public DateTime PositionTime { get; set; }
            public int Ignition { get; set; }
            public string Landmark { get; set; }
            public int TransmissionChannel { get; set; }
        }
        internal class GetPosicoesResponse
        {
            public List<GetPosicoesItemResponse> Data { get; set; }
            public int Limit { get; set; }
            public int Offset { get; set; }
            public bool IsLastPage { get; set; }
        }

    }
}


