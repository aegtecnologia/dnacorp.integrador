using MySqlConnector;
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnaCorp.Robo.Integrador.Infra.MySql
{
    public class ConexaoMySql
    {
        MySqlConnection conn;
        //public void Configura(string provider)
        //{
        //    if (string.IsNullOrEmpty(provider)) throw new Exception("Provider inválido!");
        //    conn = new MySqlConnection(provider);
        //}

        public ConexaoMySql()
        {
            string provider = "Server=10.10.100.15;User ID=interage;Password=SIGhRA@20;Database=mclient";
            conn = new MySqlConnection(provider);
        }

        private void EnsureConnectionOpen()
        {
            if (conn.State == ConnectionState.Closed)
                conn.Open();
        }

        public void Executa(string comando)
        {
            try
            {
                EnsureConnectionOpen();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = comando;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = 10000;

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {

                throw new Exception($"[classe]" +
                    $"conexao" +
                    $"[erro]" +
                    $"{ex.Message}" +
                    $"[Comando]" +
                    $"{comando}");
            }
        }

        public Int64 MaxId(string tabela, string chave, string where)
        {
            var res = RetornaDT($"select max({chave}) from {tabela} where {where}");
            if (res.Rows.Count > 0)
                return Convert.ToInt64(res.Rows[0][0]);
            else
                return 0;
        }
        public DataTable RetornaDT(string comando)
        {
            try
            {
                var cmd = new MySqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = comando;
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 10000;

                var da = new MySqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);

                return dt;

            }
            catch (Exception ex)
            {

                throw new Exception($"[classe]" +
                    $"conexao" +
                    $"[erro]" +
                    $"{ex.Message}" +
                    $"[Comando]" +
                    $"{comando}");
            }
        }
    }
}
