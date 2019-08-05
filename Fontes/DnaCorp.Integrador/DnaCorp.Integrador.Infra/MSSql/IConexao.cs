using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace DnaCorp.Integrador.Infra.MSSql
{
    public interface IConexao
    {
        void Configura(string provider);
        void Executa(string comando);
        DataTable RetornaDT(string comando);
    }
}
