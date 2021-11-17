using System;
using System.Collections.Generic;
using System.Linq;
using ShipIt.Models.ApiModels;

namespace ShipIt_DotNetCore.Helpers
{
    public static class OutboundOrdersHelper
    {
        public static int CalculateTrucksRequired(OutboundOrderRequestModel request, Dictionary<string, Product> products)
        {
            var totalWeightOfOrder = request.OrderLines.Sum(orderLine => products[orderLine.gtin].Weight * orderLine.quantity);

            var totalAmountOfTrucks = Convert.ToInt32(Math.Ceiling(totalWeightOfOrder / 2000));
            return totalAmountOfTrucks;
        }
    }
}