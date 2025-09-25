using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETL_OpinionesClientes
{
    public class Encuesta
    {
        public int IdOpinion { get; set; }
        public int IdCliente { get; set; }
        public int IdProducto { get; set; }
        public DateTime Fecha { get; set; }
        public string Comentario { get; set; }
        public string Clasificacion { get; set; }
        public int PuntajeSatisfaccion { get; set; }
        public string Fuente { get; set; }
    }
}
