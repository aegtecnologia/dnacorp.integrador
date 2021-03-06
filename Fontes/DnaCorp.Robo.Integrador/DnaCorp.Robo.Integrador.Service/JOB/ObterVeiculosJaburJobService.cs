﻿using DnaCorp.Robo.Integrador.Domain.dominios;
using DnaCorp.Robo.Integrador.Infra.MSSQL;
using DnaCorp.Robo.Integrador.Service.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DnaCorp.Robo.Integrador.Service.JOB
{
    public class ObterVeiculosJaburJobService : IObterDados
    {
        private string Endereco { get; set; }
        private string Usuario { get; set; }
        private string Senha { get; set; }
        private bool Ativo { get; set; }
        private Conexao _conexao;

        public ObterVeiculosJaburJobService()
        {
            _conexao = new Conexao();

            dynamic config = ConfigurationHelper.getConfiguration();
            var provider = Convert.ToString(config.ConnectionStrings.DefaultConnection);

            Endereco = Convert.ToString(config.Rastreadores.Jabur.Endereco);
            Usuario = Convert.ToString(config.Rastreadores.Jabur.Usuario);
            Senha = Convert.ToString(config.Rastreadores.Jabur.Senha);
            Ativo = Convert.ToBoolean(config.Rastreadores.Jabur.ObterVeiculos.Ativo);

            _conexao.Configura(provider);
        }

        public ObterDadosResponse Executa()
        {
            var response = new ObterDadosResponse();

            try
            {
                response.DataInicial = DateTime.Now;

                if (!Ativo) throw new Exception("Job inativo");
               // if (!ValidationHelper.IsValid()) throw new Exception("Job inválido");

                var veiculos = ObterVeiculos();
                PreparaBase();
                PersistirDados(veiculos);
                Criar_Log($"{nameof(ObterVeiculosJaburJobService)} - Processado com sucesso", true);

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
<login>" + Usuario + @"</login>
<senha>" + Senha + @"</senha>
</RequestVeiculo>";

            return request;
        }

        private string MontaRequisicaoItensMacro()
        {
            string request = $@"<RequestItemMacro>
<login>{Usuario}</login>
<senha>{Senha}</senha>
<todosItens>1</todosItens>
</RequestItemMacro>";

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

        public string ObterMacros()
        {
            var veiculos = new List<VeiculoJabur>();
            var request = MontaRequisicaoItensMacro();
            var xmlResponse = RequestXml(request);

            return xmlResponse;

            //ValidaRetorno(xmlResponse);

            //var xml = new XmlDocument();
            //xml.LoadXml(xmlResponse);
            //var mensagens = xml.GetElementsByTagName("Veiculo");
            //foreach (XmlNode no in mensagens)
            //{
            //    string sJson = Newtonsoft.Json.JsonConvert.SerializeXmlNode(no, Newtonsoft.Json.Formatting.None, true);
            //    dynamic msg = Newtonsoft.Json.JsonConvert.DeserializeObject(sJson);
            //    veiculos.Add(new VeiculoJabur()
            //    {
            //        VeiculoId = (int)msg.veiID,
            //        Placa = msg.placa,
            //        EspelhadoAte = msg.valEspelhamento
            //    });
            //}

            //return veiculos;
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
            var dataPartes = data.Split('/');
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
            //result = UTF8Encoding.UTF8.GetString(Decompress(buffer));
            result = Unzip(buffer);

            return result;
        }

        private HttpWebRequest CreateRequest()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Endereco);
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

        private string Unzip(byte[] zippedBuffer)
        {
            using (var zippedStream = new MemoryStream(zippedBuffer))
            {
                using (var archive = new ZipArchive(zippedStream))
                {
                    var entry = archive.Entries.FirstOrDefault();

                    if (entry != null)
                    {
                        using (var unzippedEntryStream = entry.Open())
                        {
                            using (var ms = new MemoryStream())
                            {
                                unzippedEntryStream.CopyTo(ms);
                                var unzippedArray = ms.ToArray();

                                return UTF8Encoding.UTF8.GetString(unzippedArray);
                                //return Encoding.Default.GetString(unzippedArray);
                            }
                        }
                    }

                    return null;
                }
            }
        }

    }

    
}


