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
               // if (!ValidationHelper.IsValid()) throw new Exception("Job inválido");

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
