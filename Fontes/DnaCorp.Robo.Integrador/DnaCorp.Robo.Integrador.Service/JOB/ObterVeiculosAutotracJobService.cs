﻿using DnaCorp.Robo.Integrador.Domain.dominios;
using DnaCorp.Robo.Integrador.Infra.MSSQL;
using DnaCorp.Robo.Integrador.Service.Helper;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace DnaCorp.Robo.Integrador.Service.JOB
{
    public class ObterVeiculosAutotracJobService : IObterDados
    {
        private string Endereco { get; set; }
        private string Usuario { get; set; }
        private string Senha { get; set; }
        private string Chave { get; set; }
        private string ContaEmpresa { get; set; }
        private bool Ativo { get; set; }

        private Conexao _conexao;

        public ObterVeiculosAutotracJobService()
        {
            _conexao = new Conexao();

            dynamic config = ConfigurationHelper.getConfiguration();
            var provider = Convert.ToString(config.ConnectionStrings.DefaultConnection);
            Endereco = "https://www.autotrac-online.com.br/sandboxadeapi/v1/";// Convert.ToString(config.Rastreadores.Autotrac.Endereco);
            Usuario = "teste";// Convert.ToString(config.Rastreadores.Autotrac.Usuario);
            Senha = "teste";// Convert.ToString(config.Rastreadores.Autotrac.Senha);
            Chave = Convert.ToString(config.Rastreadores.Autotrac.Chave);
            ContaEmpresa = "3253";// Convert.ToString(config.Rastreadores.Autotrac.Conta);
            Ativo = Convert.ToBoolean(config.Rastreadores.Autotrac.ObterVeiculos.Ativo);

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

                var veiculos = new List<VeiculoAutotrac>();

                ObterVeiculos(ref veiculos);

                if (veiculos.Count == 0)
                    throw new Exception("Nenhum veiculo encontrado");
                
                PersistirDados(veiculos);

                Criar_Log("Processado com sucesso", true);
                response.TotalRegistros = veiculos.Count;
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
        
        private void ObterVeiculos(ref List<VeiculoAutotrac> veiculos)
        {
            var client = new HttpClient();
            var request = $"accounts/{ContaEmpresa}/vehicles?_limit=100&_offset=0";

            client.BaseAddress = new Uri(Endereco);

            //client.DefaultRequestHeaders.Add("Content-Type", "application/json");
            //client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Chave);
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {Usuario}:{Senha}");

            HttpResponseMessage response = client.GetAsync(request).Result;

            if (!response.IsSuccessStatusCode) throw new Exception($"Falha na requisição de veiculos");

            var jsonString = response.Content.ReadAsStringAsync().Result;
            var dataResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<GetVeiculoResponse>(jsonString);

            foreach (var v in dataResponse.Data)
            {
                veiculos.Add(new VeiculoAutotrac()
                {
                    VeiculoId = v.Code,
                    Nome = v.Name,
                    Endereco = v.Address
                });
            }
            
        }

        private void PreparaBase()
        {
            var res = _conexao.RetornaDT("select * from veiculo_autotrac");
            var contemRegistros = res.Rows.Count > 0;

            if (contemRegistros) _conexao.Executa("delete veiculo_autotrac;");
        }

        private void PersistirDados(List<VeiculoAutotrac> veiculos)
        {
            PreparaBase();

            StringBuilder sb = new StringBuilder();

            foreach (var veiculo in veiculos)
            {
                sb.AppendLine($@"insert into veiculo_autotrac values (
{veiculo.VeiculoId.ToString()},
getdate(),
'{veiculo.Nome}',
'{veiculo.Endereco}'
);");
            }

            _conexao.Executa(sb.ToString());
        }

        internal class GetVeiculoItemResponse
        {
            public int Code { get; set; }
            public string Name { get; set; }
            public string Address { get; set; }
            public string UCCType { get; set; }
            public DateTime PositionTime { get; set; }
        }

        internal class GetVeiculoResponse
        {
            public List<GetVeiculoItemResponse> Data { get; set; }
            public int Limit { get; set; }
            public int Offset { get; set; }
            public bool IsLastPage { get; set; }
        }

     

    }
}