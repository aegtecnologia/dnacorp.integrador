﻿using DnaCorp.Integrador.Domain.Contratos.Job;
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
    public class ObterVeiculosSascarJobService : IObterVeiculosSascarJobService
    {
        const string wsUrl = "http://sasintegra.sascar.com.br/SasIntegra/SasIntegraWSService?wsdl";
        const string usuario = "interage";       
        const string senha = "InteragePLt@19";

        private IConexao _conexao;

        public ObterVeiculosSascarJobService(IConexao conexao)
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
                Criar_Log($"{nameof(ObterVeiculosSascarJobService)} - Processado com sucesso", true);
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
<usuario>{usuario}</usuario>
<senha>{senha}</senha>
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
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(wsUrl);
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