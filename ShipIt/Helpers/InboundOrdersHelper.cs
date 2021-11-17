using System;
using System.Collections.Generic;
using System.Linq;
using ShipIt.Models.ApiModels;
using ShipIt.Models.DataModels;

namespace ShipIt_DotNetCore.Helpers
{
    public class InboundOrdersHelper
    {
        public static Dictionary<Company, List<InboundOrderLine>> GetOrderLinesByCompany(
            IEnumerable<InboundQueryModel> allStock)
        {
            var orderlinesByCompany = new Dictionary<Company, List<InboundOrderLine>>();

            foreach (var stock in allStock)
            {
                if (stock.Held >= stock.LowerThreshold || stock.Discontinued != 0) continue;
                var orderQuantity = Math.Max(stock.LowerThreshold * 3 - stock.Held,
                    stock.MinimumOrderQuantity);

                var company = Company(stock);

                if (!orderlinesByCompany.ContainsKey(company))
                {
                    orderlinesByCompany.Add(company, new List<InboundOrderLine>());
                }

                orderlinesByCompany[company].Add(
                    new InboundOrderLine()
                    {
                        gtin = stock.Gtin,
                        name = stock.GtinName,
                        quantity = orderQuantity
                    });
            }
            Company Company(InboundQueryModel product)
            {
                var company = new Company
                {
                    Name = product.GcpName,
                    Gcp = product.Gcp,
                    Addr2 = product.Addr2,
                    Addr3 = product.Addr3,
                    Addr4 = product.Addr4,
                    PostalCode = product.PostalCode,
                    City = product.City,
                    Tel = product.Tel,
                    Mail = product.Mail
                };
                return company;
            }

            return orderlinesByCompany;
        }
        public static IEnumerable<OrderSegment> GetAllOrderSegments(Dictionary<Company, List<InboundOrderLine>> orderlinesByCompany)
        {
            var orderSegments = orderlinesByCompany.Select(orderLine => new OrderSegment()
            {
                OrderLines = orderLine.Value,
                Company = orderLine.Key
            });
            return orderSegments;
        }
    }
}