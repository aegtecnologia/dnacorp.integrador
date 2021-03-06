﻿using DnaCorp.Integrador.Domain.Contratos.Job;
using DnaCorp.Integrador.Domain.Dominios;
using DnaCorp.Integrador.Infra.MSSql;
using DnaCorp.Integrador.Service.Helper;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace DnaCorp.Integrador.Service.JOB
{
    public class ObterVeiculosAutotracJobService : IObterVeiculosAutotracJobService
    {
        private string Endereco { get; set; }
        private string Usuario { get; set; }
        private string Senha { get; set; }
        private string ContaEmpresa { get; set; }
        private bool Ativo { get; set; }

        private IConexao _conexao;

        public ObterVeiculosAutotracJobService(IConexao conexao)
        {
            _conexao = conexao ?? throw new ArgumentNullException(nameof(conexao));

            dynamic config = ConfigurationHelper.getConfiguration();
            var provider = Convert.ToString(config.ConnectionStrings.DefaultConnection);
            Endereco = Convert.ToString(config.Rastreadores.Autotrac.Endereco);
            Usuario = Convert.ToString(config.Rastreadores.Autotrac.Usuario);
            Senha = Convert.ToString(config.Rastreadores.Autotrac.Senha);
            ContaEmpresa = Convert.ToString(config.Rastreadores.Autotrac.Conta);
            Ativo = Convert.ToBoolean(config.Rastreadores.Autotrac.Ativo);

            _conexao.Configura(provider);
        }
        public void Executa()
        {
            try
            {
                if (!Ativo) throw new Exception("Job inativo");

                var veiculos = ObterVeiculos();

                PersistirDados(veiculos);

                Criar_Log("Processado com sucesso", true);
            }
            catch (Exception erro)
            {
                Criar_Log(erro.Message, false);
            }

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

        private List<VeiculoAutotrac> ObterVeiculos()
        {
            var veiculos = new List<VeiculoAutotrac>();
            var client = new HttpClient();
            var request = $"accounts/{ContaEmpresa}/vehicles";

            client.BaseAddress = new Uri(Endereco);
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {Usuario}:{Senha}");

            HttpResponseMessage response = client.GetAsync(request).Result;

            if (!response.IsSuccessStatusCode)  throw new Exception($"Falha na requisição de veiculos");
            
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

            return veiculos;
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