using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShipIt.Models.ApiModels
{
    public class OutboundOrderResponse : Response
    {
        public int TotalTrucks { get; set; }

    }
}