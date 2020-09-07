using DnaCorp.Robo.Integrador.Infra.MSSQL;
using DnaCorp.Robo.Integrador.Service.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DnaCorp.Robo.Integrador.Service.JOB
{
    public class ObterListaBrancaAutotrac : IObterDados
    {
        private string Endereco { get; set; }
        private string Usuario { get; set; }
        private string Senha { get; set; }
        private string Chave { get; set; }
        private string ContaEmpresa { get; set; }
        private bool Ativo { get; set; }

        private Conexao _conexao;

        public ObterListaBrancaAutotrac()
        {
            _conexao = new Conexao();

            dynamic config = ConfigurationHelper.getConfiguration();
            var provider = Convert.ToString(config.ConnectionStrings.DefaultConnection);
            Endereco = "https://www.autotrac-online.com.br/sandboxadeapi/v1/";// Convert.ToString(config.Rastreadores.Autotrac.Endereco);
            Usuario = "teste";// Convert.ToString(config.Rastreadores.Autotrac.Usuario);
            Senha = "teste";// Convert.ToString(config.Rastreadores.Autotrac.Senha);
            Chave = Convert.ToString(config.Rastreadores.Autotrac.Chave);
            ContaEmpresa = "3253";// Convert.ToString(config.Rastreadores.Autotrac.Conta);
            Ativo = Convert.ToBoolean(config.Rastreadores.Autotrac.ObterListaBranca.Ativo);

            _conexao.Configura(provider);
        }

        public ObterDadosResponse Executa()
        {
            var response = new ObterDadosResponse();

            try
            {
                response.DataInicial = DateTime.Now;

               // if (!ValidationHelper.IsValid()) throw new Exception("Job inválido");

                if (!Ativo) throw new Exception("Job inativo");

                EnviarListaBranca();

                Criar_Log("Processado com sucesso", true);
                response.TotalRegistros = 0;
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
'{nameof(ObterVeiculosAutotracJobService)}',
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
                LogHelper.CriarLog($"{nameof(ObterVeiculosAutotracJobService)} - {mensagem}", sucesso);
            }
        }

        private void EnviarListaBranca()
        {
            EnviarListaBrancaPorVeiculo(3253, 151);
        }
        
        private void EnviarListaBrancaPorVeiculo(int conta, int veiculoId)
        {
            var client = new HttpClient();
            var request = $"accounts/{ContaEmpresa}/whitelist/{veiculoId}";

            client.BaseAddress = new Uri(Endereco);

            client.DefaultRequestHeaders.Add("Content-Type", "application/json");
            //client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Chave);
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {Usuario}:{Senha}");

            HttpResponseMessage response = client.PostAsync(request,null).Result;

            if (!response.IsSuccessStatusCode) throw new Exception($"Falha na requisição de veiculos");
        
        }

        private void PreparaBase()
        {
            var res = _conexao.RetornaDT("select * from veiculo_autotrac");
            var contemRegistros = res.Rows.Count > 0;

            if (contemRegistros) _conexao.Executa("delete veiculo_autotrac;");
        }

       
    }
}