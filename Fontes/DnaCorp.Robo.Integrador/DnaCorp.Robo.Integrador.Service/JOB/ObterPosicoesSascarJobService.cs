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

                //Criar_Log_($"{nameof(ObterPosicoesSascarJobService)} - Processado com sucesso", true);
                response.TotalRegistros = posicoes.Count;
                response.Mensagem = "Processado com sucesso!";
            }
            catch (Exception erro)
            {
                Criar_Log_Banco(erro.Message, false);
                response.Mensagem = erro.Message;
            }
            finally
            {
                response.DataFinal = DateTime.Now;
            }

            return response;
        }
        private void Criar_Log_Banco(string mensagem, bool sucesso)
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
                Criar_Log_Arquivo(mensagem);
                //LogHelper.CriarLog(mensagem, sucesso);
            }
        }

        private string RequestXmlMock()
        {
            string retorno = "";

            using (var sr = new StreamReader(@"C:\Anderson\dnacorp.integrador\Recursos\documentos\sascar\modelos\obterPacotePosicoesJSONResponse.xml"))
            {
                retorno = sr.ReadToEnd();
            }

            return retorno;
        }

        private HashSet<SascarCadEvento> ObterSeqEventos()
        {
            var seqEventos = new HashSet<SascarCadEvento>();
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

                    var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<SascarCadEvento>(sJson);

                    seqEventos.Add(obj);
                }
            }

            return seqEventos;
        }

        public List<PosicaoSascar> ObterPosicoes()
        {
            SascarRootResponse msg = null;
            var sJson = "";
            try
            {
                var cadEventos = ObterSeqEventos();
                var posicoes = new List<PosicaoSascar>();
                var request = MontaRequisicao();
                //var xmlResponse = RequestXmlMock();
                var xmlResponse = RequestXml(request);
                ValidaRetorno(xmlResponse);

                using (MemoryStream ms = new MemoryStream())
                {
                    var xml = new XmlDocument();
                    xml.LoadXml(xmlResponse);
                    var mensagens = xml.GetElementsByTagName("return");
                    foreach (XmlNode no in mensagens)
                    {
                        msg = new SascarRootResponse();
                        sJson = "";
                        
                        sJson = no.InnerText;
                        msg = Newtonsoft.Json.JsonConvert.DeserializeObject<SascarRootResponse>(sJson);


                        var eventos = msg.eventos.Select(e => e.codigo);
                        var posicao = new PosicaoSascar(eventos.ToList(), cadEventos)
                        {
                            PosicaoId = Convert.ToInt64(msg.idPacote),
                            VeiculoId = Convert.ToInt32(msg.idVeiculo),
                            Data = msg.dataPosicao,
                            DataCadastro = DateTime.Now,
                            Latitude = msg.latitude.ToString(),
                            Longitude = msg.longitude.ToString(),
                            Cidade = msg.cidade,
                            UF = msg.uf,
                            Endereco = msg.rua,
                            Velocidade = Convert.ToInt32(msg.velocidade) 
                        };

                        posicoes.Add(posicao);


                    }
                }

                return posicoes;
            }
            catch(Exception erro)
            {
                throw new Exception($@"Erro de integraçao
Json Sascar:
{sJson}

Json Interage:
{Newtonsoft.Json.JsonConvert.SerializeObject(msg)}
");
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
0,'');");
                
                foreach (var e in p.Eventos)
                {
                    sb.AppendLine($@"insert into posicoes_sascar_eventos values (
getdate(),
{p.PosicaoId},
{e.Codigo},
'{e.EventoDescricao}',
{e.StatusCodigo},
'{e.StatusDescricao}');");
                }
            }

            try
            {
                _conexao.Executa(sb.ToString());
            }
            catch (Exception erro)
            {

                throw new Exception($@"Ocorreu um erro de persistencia.
Erro:
{erro.Message}
Comando:
{sb.ToString()}
");
            }

        }

        private string MontaRequisicao()
        {
            string request = $@"<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:web='http://webservice.web.integracao.sascar.com.br/'>
<soapenv:Header/>
<soapenv:Body>
<web:obterPacotePosicoesJSON>
<usuario>{Usuario}</usuario>
<senha>{Senha}</senha>
<quantidade>3000</quantidade>
</web:obterPacotePosicoesJSON>
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

        private void Criar_Log_Arquivo(string mensagem)
        {
            try
            {
                var pasta = @"c:/integrador/log/sascar/";
                if (!Directory.Exists(pasta))
                    Directory.CreateDirectory(pasta);

                string arquivo = $"{pasta}/log_{DateTime.Now.ToString("yyyyMMddHHmmss")}.txt";
                using (StreamWriter sw = new StreamWriter(arquivo))
                {
                    sw.WriteLine(mensagem);
                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception erro)
            {
                throw new Exception($@"Não foi possivel criar arquivo de log.
erro: {erro.Message}");
            }
        }
        
        internal class Evento
        {
            public int codigo { get; set; }
        }


        internal class SascarRootResponse
        {
            public string anguloReferencia { get; set; }
            public string bloqueio { get; set; }
            public string cidade { get; set; }
            public string codigoMacro { get; set; }
            public string conteudoMensagem { get; set; }
            public DateTime dataPacote { get; set; }
            public DateTime dataPosicao { get; set; }
            public string direcao { get; set; }
            public string distanciaReferencia { get; set; }
            public string entrada1 { get; set; }
            public string entrada2 { get; set; }
            public string entrada3 { get; set; }
            public string entrada4 { get; set; }
            public string entrada5 { get; set; }
            public string entrada6 { get; set; }
            public string entrada7 { get; set; }
            public string entrada8 { get; set; }
            public string eventoFormatado { get; set; }
            public string eventoSeqFormatado { get; set; }
            public List<Evento> eventos { get; set; }
            public string gps { get; set; }
            public string horimetro { get; set; }
            public string idPacote { get; set; }
            public string idReferencia { get; set; }
            public string idVeiculo { get; set; }
            public string ignicao { get; set; }
            public string integradoraId { get; set; }
            public string jamming { get; set; }
            public string latitude { get; set; }
            public string longitude { get; set; }
            public string memoria { get; set; }
            public string nomeMensagem { get; set; }
            public string odometro { get; set; }
            public string pontoEntrada { get; set; }
            public string pontoReferencia { get; set; }
            public string pontoSaida { get; set; }
            public string rpm { get; set; }
            public string rua { get; set; }
            public string saida1 { get; set; }
            public string saida2 { get; set; }
            public string saida3 { get; set; }
            public string saida4 { get; set; }
            public string saida5 { get; set; }
            public string saida6 { get; set; }
            public string saida7 { get; set; }
            public string saida8 { get; set; }
            public string satelite { get; set; }
            public string temperatura1 { get; set; }
            public string temperatura2 { get; set; }
            public string temperatura3 { get; set; }
            public string tensao { get; set; }
            public string textoMensagem { get; set; }
            public string tipoTeclado { get; set; }
            public string uf { get; set; }
            public string velocidade { get; set; }
        }
    }
}
