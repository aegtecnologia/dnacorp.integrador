using DnaCorp.Integrador.Domain.Contratos.Job;
using DnaCorp.Integrador.Domain.Dominios;
using DnaCorp.Integrador.Infra.MSSql;
using DnaCorp.Integrador.Service.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace DnaCorp.Integrador.Service.JOB
{
    public class ObterPosicoesOmnilinkJobService : IObterPosicoesOmnlinkJobService
    {
        const string wsUrl = "";
        const string usuario = "";
        const string senha = "";

        private IConexao _conexao;

        public ObterPosicoesOmnilinkJobService(IConexao conexao)
        {
            _conexao = conexao ?? throw new ArgumentNullException(nameof(conexao));

            dynamic config = ConfigurationHelper.getConfiguration();
            var provider = Convert.ToString(config.ConnectionStrings.DefaultConnection);

            _conexao.Configura(provider);
        }
        public void Executa()
        {
            try
            {
                var posicoes = ObterPosicoes();

                PersistirDados(posicoes);

                Criar_Log("Processado com sucesso", true);
            }
            catch (Exception erro)
            {
                Criar_Log(erro.Message, false);
            }

        }

        private void Criar_Log(string mensagem, bool sucesso)
        {
            var comando = $@"INSERT INTO LOG_AUTOMACAO VALUES(
GETDATE(),
'{nameof(ObterPosicoesOmnilinkJobService)}',
{(sucesso ? 1 : 0).ToString()},
'{mensagem.Replace("'", "")}'
)";
            _conexao.Executa(comando);

        }
        public List<PosicaoOmnilink> ObterPosicoes()
        {
            var posicoes = new List<PosicaoOmnilink>();
            var xmlResponse = RequestXmlMock();

            ValidaRetorno(xmlResponse);

            using (MemoryStream ms = new MemoryStream())
            {
                var xml = new XmlDocument();
                xml.LoadXml(xmlResponse);
                var mensagens = xml.GetElementsByTagName("TeleEvento");
                foreach (XmlNode no in mensagens)
                {
                    string sJson = Newtonsoft.Json.JsonConvert.SerializeXmlNode(no, Newtonsoft.Json.Formatting.None, true);
                    dynamic msg = Newtonsoft.Json.JsonConvert.DeserializeObject(sJson);
                    posicoes.Add(new PosicaoOmnilink()
                    {
                        TerminalId = msg.IdTerminal,
                        Data = msg.DataHoraEmissao,
                        DataCadastro = DateTime.Now,
                        Latitude = msg.Latitude,
                        Longitude = msg.Longitude,
                        Cidade = msg.Cidade,
                        UF = msg.UF,
                        Endereco = "",
                    });
                }
            }

            return posicoes;
        }

        private void ValidaRetorno(string xmlResponse)
        {
            if (string.IsNullOrEmpty(xmlResponse)) throw new Exception("Sem resposta do servidor.");

            var xml = new XmlDocument();
            xml.LoadXml(xmlResponse);
            var tags = xml.GetElementsByTagName("ErrorRequest");

            if (tags == null) return;

            foreach (XmlNode no in tags)
            {
                string sJson = Newtonsoft.Json.JsonConvert.SerializeXmlNode(no, Newtonsoft.Json.Formatting.None, true);
                dynamic msg = Newtonsoft.Json.JsonConvert.DeserializeObject(sJson);

                throw new Exception(msg.erro);
            }
        }

        private Int64 UltimoRegistro()
        {
            var consulta = "select ISNULL(MAX(POSICAOID),1) AS ULTIMO from DBO.POSICOES_OMNILINK";
            var dt = _conexao.RetornaDT(consulta);
            if (dt.Rows.Count > 0)
                return Convert.ToInt64(dt.Rows[0][0]);
            else
                return 1;
        }
        private void PersistirDados(List<PosicaoOmnilink> posicoes)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var p in posicoes)
            {
                sb.AppendLine($@"insert into POSICOES_OMNILINK values (
'{p.TerminalId.ToString()}',
'{p.DataCadastro.ToString("yyyy/MM/dd HH:mm:ss")}',
'{p.Data.ToString("yyyy/MM/dd HH:mm:ss")}',
'{p.Latitude}',
'{p.Longitude}',
'{p.UF}',
'{p.Cidade}',
'{p.Endereco?.Replace("'", "") ?? ""}');");
            }

            _conexao.Executa(sb.ToString());
        }
       
        private string RequestXmlMock()
        {
            return "";
        }

    }
}
