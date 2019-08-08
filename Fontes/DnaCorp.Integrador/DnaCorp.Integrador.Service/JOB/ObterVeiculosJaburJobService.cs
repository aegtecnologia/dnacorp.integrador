using DnaCorp.Integrador.Domain.Contratos.Job;
using DnaCorp.Integrador.Domain.Dominios;
using DnaCorp.Integrador.Infra.MSSql;
using DnaCorp.Integrador.Service.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DnaCorp.Integrador.Service.JOB
{
    public class ObterVeiculosJaburJobService : IObterVeiculosJaburJobService
    {
        const string wsUrl = "http://webservice.onixsat.com.br";
        const string usuario = "04900055000109";
        const string senha = "GV@2792!";
        //const string usuario = "03901499000104";
        //const string senha = "11032";

        private IConexao _conexao;

        public ObterVeiculosJaburJobService(IConexao conexao)
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
                var veiculos = ObterVeiculos();
                PreparaBase();
                PersistirDados(veiculos);
                Criar_Log($"{nameof(ObterVeiculosJaburJobService)} - Processado com sucesso", true);
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
'{nameof(ObterVeiculosJaburJobService)}',
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
        private void PersistirDados(List<VeiculoJabur> veiculos)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var veiculo in veiculos)
            {
                sb.AppendLine($@"insert into veiculo_jabur values (
{veiculo.VeiculoId.ToString()},
'{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}',
'{veiculo.Placa}',
'{FormataData(veiculo.EspelhadoAte).ToString("yyyy/MM/dd")}'
);");
            }

            _conexao.Executa(sb.ToString());
        }
        private string RequestXmlMock()
        {
            var pasta = @"C:\Anderson\documents\Interage\";
            var arquivo = $"{pasta}jabur-lista-veiculos-20190708170801.xml";

            using (StreamReader sr = new StreamReader(arquivo))
            {
                return sr.ReadToEnd();
            }
        }
        private string MontaRequisicao()
        {
            // obter lista de veiculos
            string request = @"<RequestVeiculo>
<login>" + usuario + @"</login>
<senha>" + senha + @"</senha>
</RequestVeiculo>";

            return request;
        }
        private List<VeiculoJabur> ObterVeiculos()
        {
            var veiculos = new List<VeiculoJabur>();
            var request = MontaRequisicao();
            var xmlResponse = RequestXml(request);

            ValidaRetorno(xmlResponse);

            var xml = new XmlDocument();
            xml.LoadXml(xmlResponse);
            var mensagens = xml.GetElementsByTagName("Veiculo");
            foreach (XmlNode no in mensagens)
            {
                string sJson = Newtonsoft.Json.JsonConvert.SerializeXmlNode(no, Newtonsoft.Json.Formatting.None, true);
                dynamic msg = Newtonsoft.Json.JsonConvert.DeserializeObject(sJson);
                veiculos.Add(new VeiculoJabur()
                {
                    VeiculoId = (int)msg.veiID,
                    Placa = msg.placa,
                    EspelhadoAte = msg.valEspelhamento
                });
            }

            return veiculos;
        }

        private bool ContemRegistros()
        {
            var res = _conexao.RetornaDT("select * from veiculo_jabur");
            return res.Rows.Count > 0;
        }

        private void PreparaBase()
        {
            if (ContemRegistros())
            {
                _conexao.Executa("delete veiculo_jabur;");
            }
        }

        private DateTime FormataData(string data)
        {
            var dataPartes = data.Split("/");
            return new DateTime(Convert.ToInt32(dataPartes[2]), Convert.ToInt32(dataPartes[1]), Convert.ToInt32(dataPartes[0]));
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
            result = UTF8Encoding.UTF8.GetString(Decompress(buffer));
            //result = UTF8Encoding.UTF8.GetString(buffer);

            return result;
        }
        private HttpWebRequest CreateRequest()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(wsUrl);
            request.Method = "POST";
            request.ContentType = "text/xml";
            return request;
        }
        private byte[] Decompress(byte[] data)
        {
            try
            {
                MemoryStream input = new MemoryStream();
                input.Write(data, 0, data.Length);
                input.Position = 0;
                GZipStream gzip = new GZipStream(input, CompressionMode.Decompress, true);
                byte[] buff = new byte[256];
                MemoryStream output = new MemoryStream();
                int read = gzip.Read(buff, 0, buff.Length);
                while (read > 0)
                {
                    output.Write(buff, 0, read);
                    read = gzip.Read(buff, 0, buff.Length);
                }
                gzip.Close();
                byte[] buffer = output.ToArray();
                output.Dispose();
                return buffer;
            }
            catch
            {
                throw new Exception("Falha ao descompactar dados");
            }
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
