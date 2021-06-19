using DnaCorp.Robo.Integrador.Infra.MSSQL;
using DnaCorp.Robo.Integrador.Service.Helper;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;


namespace DnaCorp.Robo.Integrador.Service.JOB
{
    public class ObterPosicoesRavexJobService : IObterDados
    {
        //const string URL_BASE = "http://api.risco.sistema.ravex.com.br";
        private string Endereco { get; set; }
        private string Usuario { get; set; }
        private string Senha { get; set; }
        private bool Ativo { get; set; }

        private Conexao _conexao;

        public ObterPosicoesRavexJobService()
        {
            _conexao = new Conexao();

            dynamic config = ConfigurationHelper.getConfiguration();
            var provider = Convert.ToString(config.ConnectionStrings.DefaultConnection);

            Endereco = Convert.ToString(config.Rastreadores.Ravex.Endereco);
            Usuario = Convert.ToString(config.Rastreadores.Ravex.Usuario);
            Senha = Convert.ToString(config.Rastreadores.Ravex.Senha);
            Ativo = Convert.ToBoolean(config.Rastreadores.Ravex.ObterPosicoes.Ativo);

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

                Criar_Log($"{nameof(ObterPosicoesRavexJobService)} - Processado com sucesso", true);

                response.TotalRegistros = posicoes.Count;
                response.Mensagem = "Processado com sucesso!";
            }
            catch (Exception erro)
            {
                Criar_Log(erro.Message, false);
            }

            return response;

        }
        private void PersistirDados(List<PosicaoRavex> posicoes)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var p in posicoes)
            {
                sb.AppendLine($@"insert into posicoes_ravex values (
{p.IdPosicao},
{p.IdVeiculo},
'{p.Placa}',
getdate(),
'{p.Evento_Datahora.ToString("yyyy/MM/dd HH:mm:ss")}',
'{p.GPS_Latitude}',
'{p.GPS_Longitude}',
{p.GPS_Velocidade},
{p.IdEvento},
'{p.NomeEvento}');");

            }

            _conexao.Executa(sb.ToString());
        }

        private void Criar_Log(string mensagem, bool sucesso)
        {
            try
            {
                var comando = $@"INSERT INTO LOG_AUTOMACAO VALUES(
GETDATE(),
'{nameof(ObterPosicoesRavexJobService)}',
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
                LogHelper.CriarLog(mensagem, sucesso);
            }
        }
        
        private string ObterToken()
        {
            // Add key/value
            var dict = new Dictionary<string, string>();
            dict.Add("Content-Type", "application/x-www-form-urlencoded");
            dict.Add("username", Usuario);
            dict.Add("password", Senha);
            //dict.Add("Content-Type", "application/x-www-form-urlencoded");
            //dict.Add("username", "integracaointerage@ravex.com.br");
            //dict.Add("password", "42a433fd5d00d2abc5c1871ebe95d265");

            dict.Add("grant_type", "password");

            string token = "";

            string url = Endereco + "/Token";
            
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = client.PostAsync(url, new FormUrlEncodedContent(dict)).Result;
                var responseJson = response.Content.ReadAsStringAsync().Result;

                var objToken = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseJson);

                token = objToken.access_token;
            }

            return token;
        }

        public List<PosicaoRavex> ObterPosicoes()
        {
            var idInicial = ObterUltimoRegistro();
            string token = ObterToken();
            RevexObterPosicoesResponse response = null;

            using (var client = new HttpClient())
            {
                string url = Endereco + "/api/WebServices/ObterPacotePosicoesGPRSV1?pIdInicial=" + idInicial;
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                var jsonResponse = client.GetStringAsync(url).Result;
                response = Newtonsoft.Json.JsonConvert.DeserializeObject<RevexObterPosicoesResponse>(jsonResponse);
            }

            return response.ListaPosicoes;
        }
        private Int64 ObterUltimoRegistro()
        {
            var consulta = "select ISNULL(MAX(POSICAOID),1) AS ULTIMO from DBO.POSICOES_RAVEX";
            var dt = _conexao.RetornaDT(consulta);
            if (dt.Rows.Count > 0)
                return Convert.ToInt64(dt.Rows[0][0]);
            else
                return 1;
        }

    }
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Entrada1
    {
        public string Nome { get; set; }
        public object EstadoAtual { get; set; }
        public bool Valor { get; set; }
    }

    public class Entrada2
    {
        public string Nome { get; set; }
        public object EstadoAtual { get; set; }
        public bool Valor { get; set; }
    }

    public class Entrada3
    {
        public string Nome { get; set; }
        public object EstadoAtual { get; set; }
        public bool Valor { get; set; }
    }

    public class Entrada4
    {
        public string Nome { get; set; }
        public object EstadoAtual { get; set; }
        public bool Valor { get; set; }
    }

    public class Entrada5
    {
        public string Nome { get; set; }
        public object EstadoAtual { get; set; }
        public bool Valor { get; set; }
    }

    public class Saida1
    {
        public string Nome { get; set; }
        public object EstadoAtual { get; set; }
        public bool Valor { get; set; }
    }

    public class Saida2
    {
        public string Nome { get; set; }
        public object EstadoAtual { get; set; }
        public bool Valor { get; set; }
    }

    public class Saida3
    {
        public string Nome { get; set; }
        public object EstadoAtual { get; set; }
        public bool Valor { get; set; }
    }

    public class Entrada6
    {
        public string Nome { get; set; }
        public object EstadoAtual { get; set; }
        public bool Valor { get; set; }
    }

    public class Saida4
    {
        public string Nome { get; set; }
        public object EstadoAtual { get; set; }
        public bool Valor { get; set; }
    }

    public class PosicaoRavex
    {
        public Int64 IdPosicao { get; set; }
        public int IdRastreador { get; set; }
        public int IdVeiculo { get; set; }
        public string Placa { get; set; }
        public int IdEvento { get; set; }
        public string NomeEvento { get; set; }
        public DateTime Evento_Datahora { get; set; }
        public double GPS_Latitude { get; set; }
        public double GPS_Longitude { get; set; }
        public int GPS_Direcao { get; set; }
        public int GPS_Velocidade { get; set; }
        public bool Ignicao { get; set; }
        public Entrada1 Entrada1 { get; set; }
        public Entrada2 Entrada2 { get; set; }
        public Entrada3 Entrada3 { get; set; }
        public Entrada4 Entrada4 { get; set; }
        public Entrada5 Entrada5 { get; set; }
        public Saida1 Saida1 { get; set; }
        public Saida2 Saida2 { get; set; }
        public Saida3 Saida3 { get; set; }
        public Entrada6 Entrada6 { get; set; }
        public Saida4 Saida4 { get; set; }
        public int? Temperatura1 { get; set; }
    }

    public class RevexObterPosicoesResponse
    {
        public List<PosicaoRavex> ListaPosicoes { get; set; }
        public int RetornoWSSituacao { get; set; }
    }
}



