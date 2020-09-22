using DnaCorp.Robo.Integrador.Infra.MSSQL;
using DnaCorp.Robo.Integrador.Service.Helper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Linq;

namespace DnaCorp.Robo.Integrador.Service.JOB
{
    public class ObterPosicoesSitaComJobService : IObterDados
    {
        private const string endpointUltimaPosicao = "ultimaposicao";
        private const string endpointVeiculos = "veiculos";
        private string urlBase { get; set; }
        private string login { get; set; }
        private string cgruChave { get; set; }
        private string cusuChave { get; set; }

        private SitaComVeiculosResponse memoriaVeiculos = null;

        private Conexao _conexao;

        public ObterPosicoesSitaComJobService()
        {
            _conexao = new Conexao();

            dynamic config = ConfigurationHelper.getConfiguration();
            var provider = Convert.ToString(config.ConnectionStrings.DefaultConnection);
            urlBase = "https://monitoramento.sitacom.com.br/webapi/";
            cgruChave = "4719bb3c356dfbc74c0b70bbdae47bb2";
            cusuChave = "c172e9a2ae42184a4677191c8f503e31";
            login = "Tracking";


            _conexao.Configura(provider);
        }
        public ObterDadosResponse Executa()
        {
            var response = new ObterDadosResponse();

            try
            {
                response.DataInicial = DateTime.Now;

                if (memoriaVeiculos == null || memoriaVeiculos.dataAtu < DateTime.Now.AddMinutes(-5))
                    memoriaVeiculos = ObterVeiculos();

                var posicoes = ObterPosicoes();

                if (posicoes.posicoes != null && posicoes.posicoes.Count > 0)
                    PersistirDados(posicoes);

                Criar_Log($"{nameof(ObterPosicoesSitaComJobService)} - Processado com sucesso", true);
                response.TotalRegistros = posicoes.posicoes.Count;
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
'{nameof(ObterPosicoesSitaComJobService)}',
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
        private SiteComPosicaoResponse ObterPosicoes()
        {
            var lista = new SiteComPosicaoResponse();

            var body = new SitaComCredenciais()
            {
                login = login,
                cgruChave = cgruChave,
                cusuChave = cusuChave
            };

            var json = JsonConvert.SerializeObject(body);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{urlBase}/{endpointUltimaPosicao}";
            var client = new HttpClient();

            var task = client.PostAsync(url, data);
            task.Wait();

            var response = task.Result;

            string result = response.Content.ReadAsStringAsync().Result;

            lista = JsonConvert.DeserializeObject<SiteComPosicaoResponse>(result);

            if (lista.posicoes != null && lista.posicoes.Count > 0)
            {
                lista.posicoes.ForEach(e =>
                {
                    var dataGps = e.llpoDataGps.Split(' ');
                    e.llpoDataGps = $"{Convert.ToDateTime(dataGps[0]).ToString("yyyy-MM-dd")} {dataGps[1]}";
                    var dataStatus = e.llpoDataStatus.Split(' ');
                    e.llpoDataStatus = $"{Convert.ToDateTime(dataStatus[0]).ToString("yyyy-MM-dd")} {dataStatus[1]}";
                    e.cveiPlaca = memoriaVeiculos.veiculos.Where(v => v.cveiId == e.cveiId).SingleOrDefault().cveiPlaca;
                });
            }

            return lista;
        }

        private SitaComVeiculosResponse ObterVeiculos()
        {
            var body = new SitaComCredenciais()
            {
                login = login,
                cgruChave = cgruChave,
                cusuChave = cusuChave
            };

            var json = JsonConvert.SerializeObject(body);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{urlBase}/{endpointVeiculos}";
            var client = new HttpClient();

            var task = client.PostAsync(url, data);
            task.Wait();

            var response = task.Result;

            string result = response.Content.ReadAsStringAsync().Result;
            var lista = JsonConvert.DeserializeObject<SitaComVeiculosResponse>(result);
            lista.dataAtu = DateTime.Now;

            return lista;
        }

        private void PersistirDados(SiteComPosicaoResponse lista)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var p in lista.posicoes)
            {
                sb.AppendLine($@"insert into posicoes_sitacom values (
{p.pktId},
{p.cveiId},
{p.teveId},
'{p.cveiPlaca}',
'{p.teveDescricao}',
getdate(),
'{p.llpoDataGps}',
'{p.llpoDataStatus}',
'{p.llpoLatitude.ToString()}',
'{p.llpoLongitude.ToString()}',
{p.llpoVelocidade},
'{p.truaNome}',
'{p.tmunNome}',
'{p.testNome}');");
            }

            _conexao.Executa(sb.ToString());
        }
        private string RequestMock()
        {
            return $@"";
        }

        internal class SiteComPosicaoResponse
        {
            public List<SitaComPosicao> posicoes { get; set; }
        }
        internal class SitaComPosicao
        {
            public int cveiId { get; set; }
            public string cveiPlaca { get; set; }
            public Int64 pktId { get; set; }
            public int teveId { get; set; }
            public string teveDescricao { get; set; }
            public string llpoDataGps { get; set; }
            public string llpoDataStatus { get; set; }
            public double llpoLatitude { get; set; }
            public double llpoLongitude { get; set; }
            public int llpoVelocidade { get; set; }
            public string truaNome { get; set; }
            public string tmunNome { get; set; }
            public string testNome { get; set; }
        }

        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
        internal class SitaComComando
        {
            public int teveId { get; set; }
            public string teveDescricao { get; set; }
        }

        internal class SitaComVeiculo
        {
            public int cveiId { get; set; }
            public string tveiDescricao { get; set; }
            public string cempNome { get; set; }
            public string cveiPlaca { get; set; }
            public string cveiDisplay { get; set; }
            public string cveiObs1 { get; set; }
            public string cveiObs2 { get; set; }
            public string cveiObs3 { get; set; }
            public string cveiRenavam { get; set; }
            public string cveiChassi { get; set; }
            public string cproNome { get; set; }
            public int cequSN { get; set; }
            public List<SitaComComando> comandos { get; set; }
        }

        internal class SitaComVeiculosResponse
        {
            public List<SitaComVeiculo> veiculos { get; set; }
            public DateTime dataAtu { get; set; }
        }

        internal class SitaComCredenciais
        {
            public string login { get; set; }
            public string cgruChave { get; set; }
            public string cusuChave { get; set; }
        }


    }
}