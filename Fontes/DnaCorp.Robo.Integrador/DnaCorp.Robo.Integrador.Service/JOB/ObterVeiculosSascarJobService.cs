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
    public class ObterVeiculosSascarJobService : IObterDados
    {
        private string Endereco { get; set; }
        private string Usuario { get; set; }
        private string Senha { get; set; }
        private bool Ativo { get; set; }

        private Conexao _conexao;

        public ObterVeiculosSascarJobService()
        {
            dynamic config = ConfigurationHelper.getConfiguration();
            var provider = Convert.ToString(config.ConnectionStrings.DefaultConnection);

            Endereco = Convert.ToString(config.Rastreadores.Sascar.Endereco);
            Usuario = Convert.ToString(config.Rastreadores.Sascar.Usuario);
            Senha = Convert.ToString(config.Rastreadores.Sascar.Senha);
            Ativo = Convert.ToBoolean(config.Rastreadores.Sascar.ObterVeiculos.Ativo);

            _conexao = new Conexao();
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

                var veiculos = ObterVeiculos();
                PreparaBase();
                PersistirDados(veiculos);
                Criar_Log($"{nameof(ObterVeiculosSascarJobService)} - Processado com sucesso", true);

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
'{nameof(ObterVeiculosSascarJobService)}',
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
        private void PersistirDados(List<VeiculoSascar> veiculos)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var veiculo in veiculos)
            {
                sb.AppendLine($@"insert into veiculo_sascar values (
{veiculo.VeiculoId.ToString()},
'{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}',
'{veiculo.Placa}'
);");
            }

            _conexao.Executa(sb.ToString());
        }

        private string MontaRequisicao()
        {
            string request = $@"<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:web='http://webservice.web.integracao.sascar.com.br/'>
<soapenv:Header/>
<soapenv:Body>
<web:obterVeiculos>
<usuario>{Usuario}</usuario>
<senha>{Senha}</senha>
<quantidade>1000</quantidade>
</web:obterVeiculos>
</soapenv:Body>
</soapenv:Envelope>";

            return request;
        }
        private List<VeiculoSascar> ObterVeiculos()
        {
            var veiculos = new List<VeiculoSascar>();
            var request = MontaRequisicao();
            var xmlResponse = RequestXml(request);

            ValidaRetorno(xmlResponse);

            var xml = new XmlDocument();
            xml.LoadXml(xmlResponse);
            var mensagens = xml.GetElementsByTagName("return");
            foreach (XmlNode no in mensagens)
            {
                string sJson = Newtonsoft.Json.JsonConvert.SerializeXmlNode(no, Newtonsoft.Json.Formatting.None, true);
                dynamic msg = Newtonsoft.Json.JsonConvert.DeserializeObject(sJson);
                veiculos.Add(new VeiculoSascar()
                {
                    VeiculoId = (int)msg.idVeiculo,
                    Placa = msg.placa
                });
            }

            return veiculos;
        }

        private bool ContemRegistros()
        {
            var res = _conexao.RetornaDT("select * from veiculo_sascar");
            return res.Rows.Count > 0;
        }

        private void PreparaBase()
        {
            if (ContemRegistros())
            {
                _conexao.Executa("delete veiculo_sascar;");
            }
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
    }
}