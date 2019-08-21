using DnaCorp.Robo.Integrador.Domain.dominios;
using DnaCorp.Robo.Integrador.Infra.MSSQL;
using DnaCorp.Robo.Integrador.Service.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DnaCorp.Robo.Integrador.Service.JOB
{
    public class ObterPosicoesSascarJobService : IObterDados
    {
        private string Endereco { get; set; }
        private string Usuario { get; set; }
        private string Senha { get; set; }
        private bool Ativo { get; set; }

        private Conexao _conexao;

        public ObterPosicoesSascarJobService()
        {
            _conexao = new Conexao();

            dynamic config = ConfigurationHelper.getConfiguration();
            var provider = Convert.ToString(config.ConnectionStrings.DefaultConnection);
            Endereco = Convert.ToString(config.Rastreadores.Sascar.Endereco);
            Usuario = Convert.ToString(config.Rastreadores.Sascar.Usuario);
            Senha = Convert.ToString(config.Rastreadores.Sascar.Senha);
            Ativo = Convert.ToBoolean(config.Rastreadores.Sascar.ObterPosicoes.Ativo);


            _conexao.Configura(provider);
        }
        public ObterDadosResponse Executa()
        {
            var response = new ObterDadosResponse();

            try
            {
                response.DataInicial = DateTime.Now;
                if (!Ativo) throw new Exception("Job inativo");
                if (!ValidationHelper.IsValid()) throw new Exception("Job inválido");

                var posicoes = ObterPosicoes();

                PersistirDados(posicoes);

                Criar_Log($"{nameof(ObterPosicoesSascarJobService)} - Processado com sucesso", true);
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
'{nameof(ObterPosicoesSascarJobService)}',
{(sucesso ? 1 : 0).ToString()},
'{mensagem.Replace("'", "")}'
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
                        Velocidade = Convert.ToInt32(msg.velocidade)
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
getdate(),
'{p.Data.ToString("yyyy/MM/dd HH:mm:ss")}',
'{p.Latitude}',
'{p.Longitude}',
{p.Velocidade.ToString()},
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
<usuario>{Usuario}</usuario>
<senha>{Senha}</senha>
<quantidade>3000</quantidade>
</web:obterPacotePosicoes>
</soapenv:Body>
</soapenv:Envelope>";

            return request;
        }

        private string RequestXml(string strRequest)
        {
            string result = string.Empty;


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

            return result;
        }
        private HttpWebRequest CreateRequest()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Endereco);
            request.Method = "POST";
            request.ContentType = "text/xml";
            return request;
        }
    }
}
