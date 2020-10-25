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

        private string RequestXmlMock()
        {
            string retorno = "";

            using (var sr = new StreamReader(@"C:\Anderson\dnacorp.integrador\Recursos\documentos\sascar\modelos\obterPacotePosicoesResponse.xml"))
            {
                retorno = sr.ReadToEnd();
            }

            return retorno;
        }

        private List<SascarSeqEvento> ObterSeqEventos()
        {
            var seqEventos = new List<SascarSeqEvento>();
            var xmlResponse = RequestXml_Eventos();

            ValidaRetorno(xmlResponse);

            using (MemoryStream ms = new MemoryStream())
            {
                var xml = new XmlDocument();
                xml.LoadXml(xmlResponse);
                var mensagens = xml.GetElementsByTagName("return");
                foreach (XmlNode no in mensagens)
                {
                    string sJson = Newtonsoft.Json.JsonConvert.SerializeXmlNode(no, Newtonsoft.Json.Formatting.None, true);
                 
                    var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<SascarSeqEvento>(sJson);

                    seqEventos.Add(obj);
                }
            }

            return seqEventos;
        }

        public List<PosicaoSascar> ObterPosicoes()
        {
            var seqEventos = ObterSeqEventos();
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

                    //sJson = sJson.Replace("[],", "null");
                    //dynamic msg = Newtonsoft.Json.JsonConvert.DeserializeObject(sJson);
                    //posicoes.Add(new PosicaoSascar()
                    //{
                    //    PosicaoId = Convert.ToInt64(msg.idPacote),
                    //    VeiculoId = (int)msg.idVeiculo,
                    //    Data = msg.dataPosicao,
                    //    DataCadastro = DateTime.Now,
                    //    Latitude = Convert.ToString(msg.latitude),
                    //    Longitude = Convert.ToString(msg.longitude),
                    //    Cidade = msg.cidade,
                    //    UF = msg.uf,
                    //    Endereco = msg.rua,
                    //    Velocidade = Convert.ToInt32(msg.velocidade)
                    //});

                    var msg = Newtonsoft.Json.JsonConvert.DeserializeObject<SascarRootResponse>(sJson);

                    string eventoDescricao;
                    int eventoId;

                    if (msg.eventos == null)
                    {
                        eventoId = 0;
                        eventoDescricao = "";
                    }
                    else if (msg.eventos.codigo > 0)
                    {
                        eventoId = msg.eventos.codigo;
                        eventoDescricao = seqEventos.Where(e => e.atuador.Equals(eventoId)).SingleOrDefault().descricao + " (Ativado)";
                    }
                    else
                    {
                        eventoId = msg.eventos.codigo * (-1);
                        eventoDescricao = seqEventos.Where(e => e.atuador.Equals(eventoId)).SingleOrDefault().descricao + " (Desativado)";
                        eventoId = msg.eventos.codigo;
                    }
                    posicoes.Add(new PosicaoSascar()
                    {
                        PosicaoId = msg.idPacote,
                        VeiculoId = msg.idVeiculo,
                        Data = Convert.ToDateTime(msg.dataPosicao),
                        DataCadastro = DateTime.Now,
                        Latitude = msg.latitude.ToString(),
                        Longitude = msg.longitude.ToString(),
                        Cidade = msg.cidade,
                        UF = msg.uf,
                        Endereco = msg.rua,
                        Velocidade = msg.velocidade,
                        EventoId = eventoId,
                        EventoDescricao = eventoDescricao
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
'{p.Endereco?.Replace("'", "") ?? ""}',
{p.EventoId},
'{p.EventoDescricao}');");
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

        private string RetornaXML_Atuadores()
        {
            return @"<S:Envelope xmlns:S='http://schemas.xmlsoap.org/soap/envelope/'>
   <S:Header>
      <work:WorkContext xmlns:work='http://oracle.com/weblogic/soap/workarea/'>rO0ABXdRABt3ZWJsb2dpYy5hcHAubW9kdWxvLXdlYi1lYXIAAADWAAAAI3dlYmxvZ2ljLndvcmthcmVhLlN0cmluZ1dvcmtDb250ZXh0AAV2XzIwNAAA</work:WorkContext>
   </S:Header>
   <S:Body>
      <ns0:obterGrupoAtuadoresResponse xmlns:ns0='http://webservice.web.integracao.sascar.com.br/'>
         <return>
            <descricao>Sensor Bau Lateral</descricao>
            <idAtuador>250</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Desengate</descricao>
            <idAtuador>241</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Sensor Bau</descricao>
            <idAtuador>249</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Sensor Porta Carona</descricao>
            <idAtuador>248</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Sensor Porta Motorista</descricao>
            <idAtuador>247</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Sensor Bau Traseiro</descricao>
            <idAtuador>251</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Alimentação Carreta</descricao>
            <idAtuador>227</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Sensor Braço Articulado Descarga</descricao>
            <idAtuador>256</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Violacao Painel</descricao>
            <idAtuador>231</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Falha Teclado  TD50</descricao>
            <idAtuador>541</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Sensor Porta Intermediária</descricao>
            <idAtuador>218</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Sensor Limpador</descricao>
            <idAtuador>217</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Sensor Temperatura</descricao>
            <idAtuador>219</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Sensor Lanterna</descricao>
            <idAtuador>216</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Sensor Cofre de Motor</descricao>
            <idAtuador>215</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Sensor de Betoneira</descricao>
            <idAtuador>214</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Sensor Ignição</descricao>
            <idAtuador>212</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Violação Sascarreta</descricao>
            <idAtuador>229</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Sensor Tampa de Combustível</descricao>
            <idAtuador>206</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Sensor Janelas</descricao>
            <idAtuador>207</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Panico</descricao>
            <idAtuador>517</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Sensor Capô</descricao>
            <idAtuador>257</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Sensor Capo</descricao>
            <idAtuador>242</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Sensor Braço Articulado Descarga</descricao>
            <idAtuador>243</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Sensor Portas Cabine</descricao>
            <idAtuador>246</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Sensor Freio Estacionario</descricao>
            <idAtuador>198</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Sensor Gaiola</descricao>
            <idAtuador>208</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Sensor Caracol</descricao>
            <idAtuador>209</idAtuador>
            <tipoPorta>E</tipoPorta>
         </return>
         <return>
            <descricao>Trava Bau Traseiro</descricao>
            <idAtuador>254</idAtuador>
            <tipoPorta>S</tipoPorta>
         </return>
         <return>
            <descricao>Buzzer - Segunda Instalação</descricao>
            <idAtuador>211</idAtuador>
            <tipoPorta>S</tipoPorta>
         </return>
         <return>
            <descricao>Sirene</descricao>
            <idAtuador>240</idAtuador>
            <tipoPorta>S</tipoPorta>
         </return>
         <return>
            <descricao>Bloqueio Master</descricao>
            <idAtuador>197</idAtuador>
            <tipoPorta>S</tipoPorta>
         </return>
         <return>
            <descricao>Buzzer</descricao>
            <idAtuador>232</idAtuador>
            <tipoPorta>S</tipoPorta>
         </return>
         <return>
            <descricao>Bloqueio Gradual</descricao>
            <idAtuador>220</idAtuador>
            <tipoPorta>S</tipoPorta>
         </return>
         <return>
            <descricao>Trava Bau</descricao>
            <idAtuador>252</idAtuador>
            <tipoPorta>S</tipoPorta>
         </return>
         <return>
            <descricao>Bloqueio Freio Carreta</descricao>
            <idAtuador>228</idAtuador>
            <tipoPorta>S</tipoPorta>
         </return>
         <return>
            <descricao>Bloqueio</descricao>
            <idAtuador>234</idAtuador>
            <tipoPorta>S</tipoPorta>
         </return>
         <return>
            <descricao>Trava 5 Roda</descricao>
            <idAtuador>245</idAtuador>
            <tipoPorta>S</tipoPorta>
         </return>
         <return>
            <descricao>Pisca</descricao>
            <idAtuador>233</idAtuador>
            <tipoPorta>S</tipoPorta>
         </return>
         <return>
            <descricao>Trava Bau Lateral</descricao>
            <idAtuador>253</idAtuador>
            <tipoPorta>S</tipoPorta>
         </return>
         <return>
            <descricao>Chegada Ponto</descricao>
            <idAtuador>652</idAtuador>
            <tipoPorta>V</tipoPorta>
         </return>
         <return>
            <descricao>Saida Ponto</descricao>
            <idAtuador>653</idAtuador>
            <tipoPorta>V</tipoPorta>
         </return>
         <return>
            <descricao>Excesso Tempo Parado</descricao>
            <idAtuador>563</idAtuador>
            <tipoPorta>V</tipoPorta>
         </return>
         <return>
            <descricao>GPS</descricao>
            <idAtuador>523</idAtuador>
            <tipoPorta>V</tipoPorta>
         </return>
         <return>
            <descricao>Teclado TMCD</descricao>
            <idAtuador>238</idAtuador>
            <tipoPorta>V</tipoPorta>
         </return>
         <return>
            <descricao>Ignicao</descricao>
            <idAtuador>500</idAtuador>
            <tipoPorta>V</tipoPorta>
         </return>
         <return>
            <descricao>GPS</descricao>
            <idAtuador>547</idAtuador>
            <tipoPorta>V</tipoPorta>
         </return>
         <return>
            <descricao>Teclado TD50</descricao>
            <idAtuador>224</idAtuador>
            <tipoPorta>V</tipoPorta>
         </return>
         <return>
            <descricao>Teclado TD50</descricao>
            <idAtuador>550</idAtuador>
            <tipoPorta>V</tipoPorta>
         </return>
         <return>
            <descricao>Grupo de Pontos</descricao>
            <idAtuador>555</idAtuador>
            <tipoPorta>V</tipoPorta>
         </return>
         <return>
            <descricao>Motorista Coação</descricao>
            <idAtuador>562</idAtuador>
            <tipoPorta>V</tipoPorta>
         </return>
         <return>
            <descricao>Excesso de Velocidade</descricao>
            <idAtuador>599</idAtuador>
            <tipoPorta>V</tipoPorta>
         </return>
         <return>
            <descricao>Prorrogacao de excesso de tempo parado</descricao>
            <idAtuador>662</idAtuador>
            <tipoPorta>V</tipoPorta>
         </return>
         <return>
            <descricao>Pre Sleep</descricao>
            <idAtuador>659</idAtuador>
            <tipoPorta>V</tipoPorta>
         </return>
         <return>
            <descricao>Senha Motorista</descricao>
            <idAtuador>660</idAtuador>
            <tipoPorta>V</tipoPorta>
         </return>
         <return>
            <descricao>Teclado SasMDT</descricao>
            <idAtuador>202</idAtuador>
            <tipoPorta>V</tipoPorta>
         </return>
         <return>
            <descricao>Ancora</descricao>
            <idAtuador>658</idAtuador>
            <tipoPorta>V</tipoPorta>
         </return>
         <return>
            <descricao>Teclado TD-40</descricao>
            <idAtuador>235</idAtuador>
            <tipoPorta>V</tipoPorta>
         </return>
         <return>
            <descricao>Area</descricao>
            <idAtuador>553</idAtuador>
            <tipoPorta>V</tipoPorta>
         </return>
         <return>
            <descricao>Excesso de Tempo Parado</descricao>
            <idAtuador>509</idAtuador>
            <tipoPorta>V</tipoPorta>
         </return>
         <return>
            <descricao>Pontos de referencia</descricao>
            <idAtuador>1002</idAtuador>
            <tipoPorta>V</tipoPorta>
         </return>
         <return>
            <descricao>Alerta Interno</descricao>
            <idAtuador>501</idAtuador>
            <tipoPorta>V</tipoPorta>
         </return>
      </ns0:obterGrupoAtuadoresResponse>
   </S:Body>
</S:Envelope>";
        }

        private string RequestXml_Eventos()
        {
            return @"<S:Envelope xmlns:S='http://schemas.xmlsoap.org/soap/envelope/'>
   <S:Header>
      <work:WorkContext xmlns:work='http://oracle.com/weblogic/soap/workarea/'>rO0ABXdRABt3ZWJsb2dpYy5hcHAubW9kdWxvLXdlYi1lYXIAAADWAAAAI3dlYmxvZ2ljLndvcmthcmVhLlN0cmluZ1dvcmtDb250ZXh0AAV2XzIxNwAA</work:WorkContext>
   </S:Header>
   <S:Body>
      <ns0:obterSequenciamentoEventoResponse xmlns:ns0='http://webservice.web.integracao.sascar.com.br/'>
         <return>
            <atuador>248</atuador>
            <descricao>Sensor Porta Carona</descricao>
            <idSequenciamentoEvento>2031</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>247</atuador>
            <descricao>Sensor Porta Motorista</descricao>
            <idSequenciamentoEvento>2032</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>500</atuador>
            <descricao>Ignição Desligada</descricao>
            <idSequenciamentoEvento>2033</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>500</atuador>
            <descricao>Ignição Ligada</descricao>
            <idSequenciamentoEvento>2034</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>241</atuador>
            <descricao>Desengate</descricao>
            <idSequenciamentoEvento>2035</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>251</atuador>
            <descricao>Sensor Baï¿½ 
Traseiro</descricao>
            <idSequenciamentoEvento>2036</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>553</atuador>
            <descricao>ï¿½rea de CCD</descricao>
            <idSequenciamentoEvento>2037</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>553</atuador>
            <descricao>ï¿½rea de Risco</descricao>
            <idSequenciamentoEvento>2038</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>250</atuador>
            <descricao>Sensor Baï¿½ Lateral</descricao>
            <idSequenciamentoEvento>2039</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>563</atuador>
            <descricao>Excesso Tempo Parado</descricao>
            <idSequenciamentoEvento>2041</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>547</atuador>
            <descricao>GPS</descricao>
            <idSequenciamentoEvento>2042</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>547</atuador>
            <descricao>GPS</descricao>
            <idSequenciamentoEvento>2043</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>231</atuador>
            <descricao>Violaï¿½ï¿½o Painel</descricao>
            <idSequenciamentoEvento>2046</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>517</atuador>
            <descricao>Pï¿½nico</descricao>
            <idSequenciamentoEvento>2048</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>555</atuador>
            <descricao>Parada no Cliente</descricao>
            <idSequenciamentoEvento>2050</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>562</atuador>
            <descricao>Motorista Coaï¿½ï¿½o</descricao>
            <idSequenciamentoEvento>2051</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>555</atuador>
            <descricao>Parada de Checagem</descricao>
            <idSequenciamentoEvento>2052</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>658</atuador>
            <descricao>ï¿½ncora</descricao>
            <idSequenciamentoEvento>2053</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>660</atuador>
            <descricao>Senha Motorista</descricao>
            <idSequenciamentoEvento>2056</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>659</atuador>
            <descricao>Sleep</descricao>
            <idSequenciamentoEvento>2057</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>662</atuador>
            <descricao>Prorrogaï¿½ï¿½o de Excesso de Tempo Parado</descricao>
            <idSequenciamentoEvento>2058</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>249</atuador>
            <descricao>Violação baú</descricao>
            <idSequenciamentoEvento>2059</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>675</atuador>
            <descricao>Violação 5ª Roda</descricao>
            <idSequenciamentoEvento>2060</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>674</atuador>
            <descricao>Falha na ativação automática da Trava 5ª Roda</descricao>
            <idSequenciamentoEvento>2061</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>207</atuador>
            <descricao>Sensor Janelas</descricao>
            <idSequenciamentoEvento>2072</idSequenciamentoEvento>
         </return>
         <return>
            <atuador>246</atuador>
            <descricao>Sensor Portas Cabine</descricao>
            <idSequenciamentoEvento>2074</idSequenciamentoEvento>
         </return>
      </ns0:obterSequenciamentoEventoResponse>
   </S:Body>
</S:Envelope>";
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

        internal class SascarSeqEvento
        {
            public int idSequenciamentoEvento { get; set; }
            public int atuador { get; set; }
            public string descricao { get; set; }

        }

        internal class SascarEventoResponse
        {
            public int codigo { get; set; }
        }

        internal class SascarRootResponse
        {
            public int idVeiculo { get; set; }
            public string dataPosicao { get; set; }
            //public string dataPacote { get; set; }
            public double latitude { get; set; }
            public double longitude { get; set; }
            //public int direcao { get; set; }
            public int velocidade { get; set; }
            //public int ignicao { get; set; }
            //public int odometro { get; set; }
            //public int horimetro { get; set; }
            //public int tensao { get; set; }
            //public int saida1 { get; set; }
            //public int saida2 { get; set; }
            //public int saida3 { get; set; }
            //public int saida4 { get; set; }
            //public int entrada1 { get; set; }
            //public int entrada2 { get; set; }
            //public int entrada3 { get; set; }
            //public int entrada4 { get; set; }
            //public int satelite { get; set; }
            //public int memoria { get; set; }
            //public int idReferencia { get; set; }
            //public int bloqueio { get; set; }
            //public int gps { get; set; }
            public string uf { get; set; }
            public string cidade { get; set; }
            public string rua { get; set; }
            //public string pais { get; set; }
            //public string pontoReferencia { get; set; }
            //public int anguloReferencia { get; set; }
            //public int distanciaReferencia { get; set; }
            //public int rpm { get; set; }
            //public int temperatura1 { get; set; }
            //public int temperatura2 { get; set; }
            //public int temperatura3 { get; set; }
            //public int saida5 { get; set; }
            //public int saida6 { get; set; }
            //public int saida7 { get; set; }
            //public int saida8 { get; set; }
            //public int entrada5 { get; set; }
            //public int entrada6 { get; set; }
            //public int entrada7 { get; set; }
            //public int entrada8 { get; set; }
            //public int pontoEntrada { get; set; }
            //public int pontoSaida { get; set; }
            //public int codigoMacro { get; set; }
            //public string nomeMensagem { get; set; }
            //public string conteudoMensagem { get; set; }
            //public string textoMensagem { get; set; }
            //public int tipoTeclado { get; set; }
            //public List<object> eventoSequenciamento { get; set; }
            public SascarEventoResponse eventos { get; set; }
            //public int jamming { get; set; }
            public Int64 idPacote { get; set; }
            //public int integradoraId { get; set; }
            //public object idMotorista { get; set; }
            //public string nomeMotorista { get; set; }
            //public object estadoLimpadorParabrisa { get; set; }
        }
    }
}
