using Hangfire.Server;
using System;
using System.Collections.Generic;
using System.Text;

namespace DnaCorp.Integrador.Domain.Contratos.Job
{
    public interface ITesteJobService
    {
        void Executa(PerformContext context);
    }
}
