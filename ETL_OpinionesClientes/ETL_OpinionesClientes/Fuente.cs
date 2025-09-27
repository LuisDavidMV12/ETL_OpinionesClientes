using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETL_OpinionesClientes
{
    
        public class Fuente
        {
            public string IdFuente { get; set; }
            public string TipoFuente { get; set; }
            public DateTime FechaCarga { get; set; }
        }
    
}
