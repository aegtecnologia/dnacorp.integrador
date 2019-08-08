﻿using DnaCorp.Integrador.Domain.Contratos.Job;
using DnaCorp.Integrador.Domain.Dominios;
using DnaCorp.Integrador.Infra.MSSql;
using DnaCorp.Integrador.Service.Helper;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Xml;

namespace DnaCorp.Integrador.Service.JOB
{
    public class ObterPosicoesJaburJobService : IObterPosicoesJaburJobService
    {
        const string wsUrl = "http://webservice.onixsat.com.br";
        //const string usuario = "04900055000109";
        //const string senha = "GV@2792!";
        const string usuario = "03901499000104";      
        const string senha = "11032";

        private IConexao _conexao;

        public ObterPosicoesJaburJobService(IConexao conexao)
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
'ObterPosicoesJaburJobService',
{(sucesso ? 1:0).ToString()},
'{mensagem.Replace("'", "")}'
)";
            _conexao.Executa(comando);

        }
        public List<PosicaoJabur> ObterPosicoes()
        {
            var posicoes = new List<PosicaoJabur>();
            var request = MontaRequisicao();
            var xmlResponse = RequestXml(request);

            ValidaRetorno(xmlResponse);

            using (MemoryStream ms = new MemoryStream())
            {
                var xml = new XmlDocument();
                xml.LoadXml(xmlResponse);
                var mensagens = xml.GetElementsByTagName("MensagemCB");
                foreach (XmlNode no in mensagens)
                {
                    string sJson = Newtonsoft.Json.JsonConvert.SerializeXmlNode(no, Newtonsoft.Json.Formatting.None, true);
                    dynamic msg = Newtonsoft.Json.JsonConvert.DeserializeObject(sJson);
                    posicoes.Add(new PosicaoJabur()
                    {
                        PosicaoId = Convert.ToInt64(msg.mId),
                        VeiculoId = (int)msg.veiID,
                        Data = msg.dt,
                        DataCadastro = DateTime.Now,
                        Latitude = msg.lat,
                        Longitude = msg.lon,
                        Cidade = msg.mun,
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

        private Int64 UltimoRegistro()
        {
            var consulta = "select ISNULL(MAX(POSICAOID),1) AS ULTIMO from DBO.POSICOES_JABUR";
            var dt = _conexao.RetornaDT(consulta);
            if (dt.Rows.Count > 0)
                return Convert.ToInt64(dt.Rows[0][0]);
            else
                return 1;
        }
        private void PersistirDados(List<PosicaoJabur> posicoes)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var p in posicoes)
            {
                sb.AppendLine($@"insert into posicoes_jabur values (
{p.PosicaoId},
{p.VeiculoId.ToString()},
'{p.DataCadastro.ToString("yyyy/MM/dd HH:mm:ss")}',
'{p.Data.ToString("yyyy/MM/dd HH:mm:ss")}',
'{p.Latitude}',
'{p.Longitude}',
'{p.UF}',
'{p.Cidade}',
'{p.Endereco?.Replace("'","") ?? ""}');");
            }

            _conexao.Executa(sb.ToString());
        }   
        private string MontaRequisicao()
        {
            var id = UltimoRegistro().ToString();//"46062233155";

            string comando = @"<RequestMensagemCB>
<login>" + usuario + @"</login>
<senha>" + senha + @"</senha>
<mId>" + id + @"</mId>
</RequestMensagemCB>";

            return comando;
        }
        private string RequestXmlMock()
        {
            //ToDo
            var pasta = @"C:\Anderson\dnacorp.integrador\Recursos\documentos\jabur\modelos\";
            var arquivo = $"{pasta}jabur-mensagens-20190708171146.xml";

            using (StreamReader sr = new StreamReader(arquivo))
            {
                return sr.ReadToEnd();
            }
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
                result = UTF8Encoding.UTF8.GetString(Decompress(buffer));
            }
            catch (Exception ex)
            {
                // tratar exceção
            }
            return result;
        }
        private HttpWebRequest CreateRequest()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(wsUrl);//"http://webservice.onixsat.com.br");
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
        private DateTime FormataData(string data)
        {
            var dataPartes = data.Split("/");
            return new DateTime(Convert.ToInt32(dataPartes[2]), Convert.ToInt32(dataPartes[1]), Convert.ToInt32(dataPartes[0]));
        }

    }
}
