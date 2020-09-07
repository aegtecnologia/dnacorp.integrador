using DnaCorp.Robo.Integrador.Domain.dominios;
using DnaCorp.Robo.Integrador.Infra.MSSQL;
using DnaCorp.Robo.Integrador.Infra.MySql;
using DnaCorp.Robo.Integrador.Service.Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DnaCorp.Robo.Integrador.Service.JOB
{
    public class ObterPosicoesSighraJobService : IObterDados
    {
        //private string Endereco { get; set; }
        //private string Usuario { get; set; }
        //private string Senha { get; set; }
        //private bool Ativo { get; set; }

        private Conexao _conexaoInterage;
        private ConexaoMySql _conexaoSigrha;

        public ObterPosicoesSighraJobService()
        {
            _conexaoInterage = new Conexao();
            _conexaoSigrha = new ConexaoMySql();

            dynamic config = ConfigurationHelper.getConfiguration();
            var provider = Convert.ToString(config.ConnectionStrings.DefaultConnection);
            //Endereco = Convert.ToString(config.Rastreadores.Omnilink.Endereco);
            //Usuario = Convert.ToString(config.Rastreadores.Omnilink.Usuario);
            //Senha = Convert.ToString(config.Rastreadores.Omnilink.Senha);
            //Ativo = Convert.ToBoolean(config.Rastreadores.Omnilink.ObterPosicoes.Ativo);


            _conexaoInterage.Configura(provider);
        }
        public ObterDadosResponse Executa()
        {
            var response = new ObterDadosResponse();

            try
            {
                response.DataInicial = DateTime.Now;

                var veiculos = ObterVeiculosFromDb();
                var posicoes = ObterPosicoesFromDb();

                PersistirDados(posicoes, veiculos);

                Criar_Log($"{nameof(ObterPosicoesOmnilinkJobService)} - Processado com sucesso", true);
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

        
        private List<ObterVeiculosSighraResponse> ObterVeiculosFromDb()
        {

            var veiculos = new List<ObterVeiculosSighraResponse>();

            var sql = $@"select cvei_id, cvei_placa from cad_veiculo";

            var dt = _conexaoSigrha.RetornaDT(sql);

            foreach (DataRow dr in dt.Rows)
            {
                var p = new ObterVeiculosSighraResponse()
                {
                    cvei_id = Convert.ToInt32(dr["cvei_id"]),
                    cvei_placa = dr["cvei_placa"].ToString()
                };

                veiculos.Add(p);
            }

            return veiculos;
        }
        private List<ObterPosicoesSighraResponse> ObterPosicoesFromDb()
        {

            var posicoes = new List<ObterPosicoesSighraResponse>();
            
            var sql = $@"select * from log_ultima_posicao";

            var dt = _conexaoSigrha.RetornaDT(sql);

            foreach (DataRow dr in dt.Rows)
            {
                var p = new ObterPosicoesSighraResponse()
                {
                    lupo_sequencia = Convert.ToInt32(dr["lupo_sequencia"]),
                    lupo_cvei_id = Convert.ToInt32(dr["lupo_cvei_id"]),
                    lupo_data_gps = Convert.ToDateTime(dr["lupo_data_gps"]),
                    lupo_data_status = Convert.ToDateTime(dr["lupo_data_status"]),
                    lupo_velocidade = Convert.ToInt32(dr["lupo_velocidade"]),
                    lupo_latitude = dr["lupo_latitude"].ToString(),
                    lupo_longitude = dr["lupo_longitude"].ToString()
                };

                posicoes.Add(p);
            }

            return posicoes;
        }

        private void Criar_Log(string mensagem, bool sucesso)
        {
            try
            {
                var comando = $@"INSERT INTO LOG_AUTOMACAO VALUES(
GETDATE(),
'{nameof(ObterPosicoesSighraJobService)}',
{(sucesso ? 1 : 0).ToString()},
'{mensagem.Replace("'", "")}'
)";
                _conexaoInterage.Executa(comando);

            }
            catch (Exception erro)
            {
                mensagem = erro.Message;
            }
            finally
            {
                LogHelper.CriarLog(mensagem, sucesso);
            }
        }

        

        private void PersistirDados(List<ObterPosicoesSighraResponse> posicoes, List<ObterVeiculosSighraResponse> veiculos)
        {
            StringBuilder sb = new StringBuilder();

            int contador = 0;

            foreach (var p in posicoes)
            {
                var placa = veiculos.Where(e => e.cvei_id == p.lupo_cvei_id).SingleOrDefault().cvei_placa;

                sb.AppendLine($@"insert into posicoes_sighra values (
{p.lupo_sequencia},
{p.lupo_cvei_id},
'{placa}',
getdate(),
'{p.lupo_data_gps.ToString("yyyy-MM-dd HH:mm:ss")}',
'{p.lupo_data_status.ToString("yyyy-MM-dd HH:mm:ss")}',
'{p.lupo_latitude}',
'{p.lupo_longitude}',
{p.lupo_velocidade.ToString()});");

                contador++;

                if (contador > 99 && sb.Length > 0)
                {
                    _conexaoInterage.Executa(sb.ToString());
                    sb.Clear();
                    contador = 0;
                    System.Threading.Thread.Sleep(3000);
                }

            }

            if (sb.Length > 0)
                _conexaoInterage.Executa(sb.ToString());
        }
        
        internal class ObterPosicoesSighraResponse
        {
            public int lupo_sequencia { get; set; }
            public int lupo_cvei_id { get; set; }
            public DateTime lupo_data_gps { get; set; }
            public DateTime lupo_data_status { get; set; }           
            public string lupo_latitude { get; set; }
            public string lupo_longitude { get; set; }
            public int lupo_velocidade { get; set; }
        }

        internal class ObterVeiculosSighraResponse
        {
            public int cvei_id { get; set; }
            public string cvei_placa { get; set; }
        }
    }
}