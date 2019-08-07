using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

namespace DnaCorp.Integrador.Infra.MSSql
{
    public class Conexao : IConexao
    {
        SqlConnection conn;
        public void Configura(string provider)
        {
            if (string.IsNullOrEmpty(provider))  throw new Exception("Provider inválido!");
            conn = new SqlConnection(provider);
        }

        private void EnsureConnectionOpen()
        {
            if (conn.State == ConnectionState.Closed)
                conn.Open();
        }

        public void Executa(string comando)
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

        public DataTable RetornaDT(string comando)
        {
            var cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.CommandText = comando;
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 10000;

            var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);

            return dt;
        }
    }
}
