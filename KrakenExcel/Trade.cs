using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KrakenExcel
{
    public class Trade
    {
        public string ordertxid { get; set; }
        public string postxid { get; set; }
        public string pair { get; set; }
        public float time { get; set; }
        public string type { get; set; }
        public string ordertype { get; set; }
        public string price { get; set; }
        public string cost { get; set; }
        public string fee { get; set; }
        public string vol { get; set; }
        public string margin { get; set; }
        public string misc { get; set; }
    }

}
