using DnaCorp.Integrador.Domain.Contratos.Job;
using DnaCorp.Integrador.Domain.Dominios;
using DnaCorp.Integrador.Infra.MSSql;
using DnaCorp.Integrador.Service.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace DnaCorp.Integrador.Service.JOB
{
    public class ObterPosicoesSascarJobService : IObterPosicoesSascarJobService
    {
        const string wsUrl = "http://sasintegra.sascar.com.br/SasIntegra/SasIntegraWSService?wsdl";
        const string usuario = "interage";
        const string senha = "sascar";
        
        private IConexao _conexao;

        public ObterPosicoesSascarJobService(IConexao conexao)
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
'{nameof(ObterPosicoesSascarJobService)}',
{(sucesso ? 1 : 0).ToString()},
'{mensagem.Replace("'", "")}'
)";
            _conexao.Executa(comando);

        }
        public List<PosicaoSascar> ObterPosicoes()
        {
            var posicoes = new List<PosicaoSascar>();
            var request = MontaRequisicao();
            var xmlResponse = RequestXml(request);

            ValidaRetorno(xmlResponse);

            using (MemoryStream ms = new MemoryStream())
            {
                var xml = new XmlDocument();
                xml.LoadXml(xmlResponse);
                var mensagens = xml.GetElementsByTagName("return");
                foreach (XmlNode no in mensagens)
                {
                    string sJson = Newtonsoft.Json.JsonConvert.SerializeXmlNode(no, Newtonsoft.Json.Formatting.None, true);
                    dynamic msg = Newtonsoft.Json.JsonConvert.DeserializeObject(sJson);
                    posicoes.Add(new PosicaoSascar()
                    {
                        PosicaoId = Convert.ToInt64(msg.idPacote),
                        VeiculoId = (int)msg.idVeiculo,
                        Data = msg.dataPosicao,
                        DataCadastro = DateTime.Now,
                        Latitude = Convert.ToString(msg.latitude),
                        Longitude = Convert.ToString(msg.longitude),
                        Cidade = msg.cidade,
                        UF = msg.uf,
                        Endereco = msg.rua,
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

        private void PersistirDados(List<PosicaoSascar> posicoes)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var p in posicoes)
            {
                sb.AppendLine($@"insert into posicoes_sascar values (
{p.PosicaoId},
{p.VeiculoId.ToString()},
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
        private string MontaRequisicao()
        {
            string request = $@"<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:web='http://webservice.web.integracao.sascar.com.br/'>
<soapenv:Header/>
<soapenv:Body>
<web:obterPacotePosicoes>
<usuario>{usuario}</usuario>
<senha>{senha}</senha>
<quantidade>3000</quantidade>
</web:obterPacotePosicoes>
</soapenv:Body>
</soapenv:Envelope>";

            return request;
        }
        
        private string RequestXml(string strRequest)
        {
            string result = string.Empty;
            try
            {
                // requisição xml em bytes

                byte[] sendData = UTF8Encoding.UTF8.GetBytes(strRequest);
                // cria requisicao
                HttpWebRequest request = CreateRequest();
                Stream requestStream = request.GetRequestStream();
                // envia requisição
                requestStream.Write(sendData, 0, sendData.Length);
                requestStream.Flush();
                requestStream.Dispose();
                // captura resposta
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                MemoryStream output = new MemoryStream();
                byte[] buffer = new byte[256];
                int byteReceived = -1;
                do
                {
                    byteReceived = responseStream.Read(buffer, 0, buffer.Length);
                    output.Write(buffer, 0, byteReceived);
                } while (byteReceived > 0);
                responseStream.Dispose();
                response.Close();
                buffer = output.ToArray();
                output.Dispose();
                // transforma resposta em string para leitura xml
                result = UTF8Encoding.UTF8.GetString(buffer);
            }
            catch (Exception ex)
            {
                // tratar exceção
            }
            return result;
        }
        private HttpWebRequest CreateRequest()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(wsUrl);
            request.Method = "POST";
            request.ContentType = "text/xml";
            return request;
        }
    }
}
