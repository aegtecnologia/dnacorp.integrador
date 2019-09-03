﻿using DnaCorp.Robo.Integrador.Domain.dominios;
using DnaCorp.Robo.Integrador.Infra.MSSQL;
using DnaCorp.Robo.Integrador.Service.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DnaCorp.Robo.Integrador.Service.JOB
{
    public class ObterPosicoesOmnilinkJobService : IObterDados
    {
        private string Endereco { get; set; }
        private string Usuario { get; set; }
        private string Senha { get; set; }
        private bool Ativo { get; set; }

        private Conexao _conexao;

        public ObterPosicoesOmnilinkJobService()
        {
            _conexao = new Conexao();

            dynamic config = ConfigurationHelper.getConfiguration();
            var provider = Convert.ToString(config.ConnectionStrings.DefaultConnection);
            Endereco = Convert.ToString(config.Rastreadores.Omnilink.Endereco);
            Usuario = Convert.ToString(config.Rastreadores.Omnilink.Usuario);
            Senha = Convert.ToString(config.Rastreadores.Omnilink.Senha);
            Ativo = Convert.ToBoolean(config.Rastreadores.Omnilink.ObterPosicoes.Ativo);


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

                // PersistirDados(posicoes);

                //Criar_Log($"{nameof(ObterPosicoesOmnilinkJobService)} - Processado com sucesso", true);
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
'{nameof(ObterPosicoesOmnilinkJobService)}',
{(sucesso ? 1 : 0).ToString()},
'{mensagem.Replace("'", "")}'
)";
                //_conexao.Executa(comando);

            }
            catch (Exception erro)
            {
                mensagem = erro.Message;
            }
            finally
            {
                //LogHelper.CriarLog(mensagem, sucesso);
            }
        }
        private List<ObterPosicoesOmnilinkResponse> ObterPosicoes()
        {
            var posicoes = new List<ObterPosicoesOmnilinkResponse>();
            var request = MontaRequisicao();
            //var xmlResponse = RequestXml(request);

            
            //ValidaRetorno(xmlResponse);

            var xml = new XmlDocument();
            xml.Load(@"c:\anderson\omnilink-request-teste.xml");
            //xml.LoadXml(xmlResponse);
            //xml.Save(@"c:\anderson\omnilink-request.xml");

            //throw new Exception("teste");

            var mensagens = xml.GetElementsByTagName("TeleEvento");
            foreach (XmlNode no in mensagens)
            {
                string sJson = Newtonsoft.Json.JsonConvert.SerializeXmlNode(no, Newtonsoft.Json.Formatting.None, true);
                dynamic msg = Newtonsoft.Json.JsonConvert.DeserializeObject(sJson);
                var p = new ObterPosicoesOmnilinkResponse()
                {
                    NumeroSequencia = msg.NumeroSequencia,
                    DataHoraEmissao = msg.DataHoraEmissao,
                    IdTerminal = msg.IdTerminal,
                    Latitude = msg.Latitude,
                    Longitude = msg.Longitude,
                    Localizacao = msg.Localizacao,
                    Velocidade = msg.Velocidade
                };

                p.TratarDados();

                posicoes.Add(p);
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
            string request = $@"<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:web='http://microsoft.com/webservices/'>
<soapenv:Header/>
<soapenv:Body>
        <web:ObtemEventosNormais> 
             <web:Usuario>Interage</web:Usuario>
                   <web:Senha>Dna@56%</web:Senha>               
                        <web:UltimoSequencial>2147483647</web:UltimoSequencial>                    
                          </web:ObtemEventosNormais>                     
                        </soapenv:Body>
                      </soapenv:Envelope>
                       ";

            return request;
        }

        private string RequestXmlMock()
        {
            var xml = @"<?xml version='1.0' encoding='utf - 8'?><soap:Envelope xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema'><soap:Body><ObtemEventosNormaisResponse xmlns='http://microsoft.com/webservices/'><ObtemEventosNormaisResult> &lt;TeleEvento&gt; &lt;NumeroSequencia&gt;30673&lt;/NumeroSequencia&gt; &lt;IdSeqMsg&gt; 51778 &lt;/IdSeqMsg&gt;  &lt;Origem&gt; 0 &lt;/Origem&gt;  &lt;Destino&gt; 0 &lt;/Destino&gt;  &lt;TipoMsg&gt; 1 &lt;/TipoMsg&gt;  &lt;CodMsg&gt; 92 &lt;/CodMsg&gt;  &lt;DataHoraEmissao&gt; 03/09/2019 18:51:26 &lt;/DataHoraEmissao&gt;  &lt;Prioridade&gt; 0 &lt;/Prioridade&gt;  &lt;TamanhoMensagem&gt; 48 &lt;/TamanhoMensagem&gt;  &lt;IdTerminal&gt; D128C &lt;/IdTerminal&gt;  &lt;Versao_Protocolo&gt; 03 &lt;/Versao_Protocolo&gt;  &lt;StatusVeic&gt; 2 &lt;/StatusVeic&gt;  &lt;DataHoraEvento&gt; 03/09/2019 17:57:15 &lt;/DataHoraEvento&gt;  &lt;Ignicao&gt; 1 &lt;/Ignicao&gt;  &lt;Validade&gt; 0 &lt;/Validade&gt;  &lt;Rumo&gt; 6 &lt;/Rumo&gt;  &lt;Velocidade&gt; 86 &lt;/Velocidade&gt;  &lt;Latitude&gt; 021_55_10_6_S &lt;/Latitude&gt;  &lt;Longitude&gt; 045_36_10_0_W &lt;/Longitude&gt;  &lt;Hodometro&gt; 568476 &lt;/Hodometro&gt;  &lt;Intervalo&gt; 7 &lt;/Intervalo&gt;  &lt;IntervaloDif&gt; 255 &lt;/IntervaloDif&gt;  &lt;LacreCarreta&gt; 1 &lt;/LacreCarreta&gt;  &lt;LacreCabine&gt; 1 &lt;/LacreCabine&gt;  &lt;LacreBau&gt; 0 &lt;/LacreBau&gt;  &lt;FalhaAbend&gt; 0 &lt;/FalhaAbend&gt;  &lt;FalhaFlash&gt; 0 &lt;/FalhaFlash&gt;  &lt;HodoInop&gt; 0 &lt;/HodoInop&gt;  &lt;PerdaGPS&gt; 0 &lt;/PerdaGPS&gt;  &lt;BotaoPanico&gt; 0 &lt;/BotaoPanico&gt;  &lt;PortaBau&gt; 1 &lt;/PortaBau&gt;  &lt;PortaDireita&gt; 1 &lt;/PortaDireita&gt;  &lt;PortaEsquerda&gt; 1 &lt;/PortaEsquerda&gt;  &lt;EngateCarreta&gt; 1 &lt;/EngateCarreta&gt;  &lt;ChaveDesbloqueio&gt; 0 &lt;/ChaveDesbloqueio&gt;  &lt;BotaoBau&gt; 2 &lt;/BotaoBau&gt;  &lt;EstadoTerminal&gt; 1 &lt;/EstadoTerminal&gt;  &lt;FlagFalhaTravaMot&gt; 0 &lt;/FlagFalhaTravaMot&gt;  &lt;FalhaTravaMot&gt; 2 &lt;/FalhaTravaMot&gt;  &lt;BatExtOut&gt; 0 &lt;/BatExtOut&gt;  &lt;BatIntOut&gt; 0 &lt;/BatIntOut&gt;  &lt;ChaveArmadilha&gt; 0 &lt;/ChaveArmadilha&gt;  &lt;Historico&gt; 1 &lt;/Historico&gt;  &lt;Tecnologia&gt; 2 &lt;/Tecnologia&gt;  &lt;DataHoraCnx&gt;  &lt;/DataHoraCnx&gt;  &lt;Serial&gt; 0 &lt;/Serial&gt;  &lt;IdSeqVeiculo&gt; 94002 &lt;/IdSeqVeiculo&gt;  &lt;IP&gt; 10.176.18.235 &lt;/IP&gt;  &lt;Port&gt; 0 &lt;/Port&gt;  &lt;Intervalo_OP&gt; 0 &lt;/Intervalo_OP&gt;  &lt;Intervalo_IP_SMS&gt; 120 &lt;/Intervalo_IP_SMS&gt;  &lt;TecnologiaIntervalo&gt; 2 &lt;/TecnologiaIntervalo&gt;  &lt;UsandoDataHoraLES&gt; 0 &lt;/UsandoDataHoraLES&gt;  &lt;Central_CFG_Rast&gt; 1 &lt;/Central_CFG_Rast&gt;  &lt;Id_Terminal_Sat&gt; 0 &lt;/Id_Terminal_Sat&gt;  &lt;DataHoraLES&gt; 0 &lt;/DataHoraLES&gt;  &lt;MessageID&gt; 0 &lt;/MessageID&gt;  &lt;CentralIntervalo&gt; 0 &lt;/CentralIntervalo&gt;  &lt;EvtSateliteDuplicado&gt; 0 &lt;/EvtSateliteDuplicado&gt;  &lt;Operadora&gt; 2 &lt;/Operadora&gt;  &lt;ModeloRastreador&gt; 8 &lt;/ModeloRastreador&gt;  &lt;Localizacao&gt; 3,10 km a SSO de Sao Goncalo do Sapucai - MG &lt;/Localizacao&gt;  &lt;/TeleEvento&gt;</ObtemEventosNormaisResult></ObtemEventosNormaisResponse></soap:Body></soap:Envelope>";
            return xml;
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

        internal class ObterPosicoesOmnilinkResponse
        {
            public string NumeroSequencia { get; set; }
            public string DataHoraEmissao { get; set; }
            public string IdTerminal { get; set; }
            public string Latitude { get; set; }
            public string Longitude { get; set; }
            public string Localizacao { get; set; }
            public string Velocidade { get; set; }

            public void TratarDados()
            {
                NumeroSequencia = NumeroSequencia.TrimEnd().TrimStart();
                DataHoraEmissao= DataHoraEmissao.TrimEnd().TrimStart();
                IdTerminal = IdTerminal.TrimEnd().TrimStart();
                Latitude = Latitude.TrimEnd().TrimStart();
                Longitude = Longitude.TrimEnd().TrimStart();
                Localizacao = Localizacao.TrimEnd().TrimStart();
                Velocidade = Velocidade.TrimEnd().TrimStart();

                string[] dataHora = DataHoraEmissao.Split(' ');
                string[] diaMesAno = dataHora[0].Split('/');
                DataHoraEmissao = $"{diaMesAno[2]}-{diaMesAno[1]}-{diaMesAno[0]} {dataHora[1]}";
                IdTerminal = Convert.ToInt32(IdTerminal, 16).ToString();


            }
         
        }
    }
}
