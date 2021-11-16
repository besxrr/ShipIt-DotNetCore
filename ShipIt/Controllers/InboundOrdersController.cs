﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Models.DataModels;
using ShipIt.Repositories;

namespace ShipIt.Controllers
{
    [Route("orders/inbound")]
    public class InboundOrderController : ControllerBase
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);

        private readonly IEmployeeRepository _employeeRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IProductRepository _productRepository;
        private readonly IStockRepository _stockRepository;

        public InboundOrderController(IEmployeeRepository employeeRepository, ICompanyRepository companyRepository, IProductRepository productRepository, IStockRepository stockRepository)
        {
            _employeeRepository = employeeRepository;
            _stockRepository = stockRepository;
            _companyRepository = companyRepository;
            _productRepository = productRepository;
        }

        [HttpGet("{warehouseId}")]
        public InboundOrderResponse Get([FromRoute] int warehouseId)
        {
            Log.Info("orderIn for warehouseId: " + warehouseId);

            var operationsManager = new Employee(_employeeRepository.GetOperationsManager(warehouseId));

            Log.Debug($"Found operations manager: {operationsManager}");

            var allStock = _stockRepository.QueryIncomingByWarehouseId(warehouseId);

            var orderLinesByCompany = new Dictionary<Company, List<InboundOrderLine>>();
            
            foreach (var product in allStock)
            {
                if(product.Held < product.LowerThreshold && product.Discontinued == 0)
                {
                    var orderQuantity = Math.Max(product.LowerThreshold * 3 - product.Held, product.MinimumOrderQuantity);
                    var company = Company(product);
                    
                    if (!orderLinesByCompany.ContainsKey(company))
                    {
                        orderLinesByCompany.Add(company, new List<InboundOrderLine>());
                    }

                    orderLinesByCompany[company].Add( 
                        new InboundOrderLine()
                        {
                            gtin = product.Gtin,
                            name = product.GcpName,
                            quantity = orderQuantity
                        });
                }
            }

            Log.Debug($"Constructed order lines: {orderLinesByCompany}");

            var orderSegments = orderLinesByCompany.Select(ol => new OrderSegment()
            {
                OrderLines = ol.Value,
                Company = ol.Key
            });

            Log.Info("Constructed inbound order");

            return new InboundOrderResponse()
            {
                OperationsManager = operationsManager,
                WarehouseId = warehouseId,
                OrderSegments = orderSegments
            };

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
        }

        [HttpPost("")]
        public void Post([FromBody] InboundManifestRequestModel requestModel)
        {
            Log.Info("Processing manifest: " + requestModel);

            var gtins = new List<string>();

            foreach (var orderLine in requestModel.OrderLines)
            {
                if (gtins.Contains(orderLine.gtin))
                {
                    throw new ValidationException($"Manifest contains duplicate product gtin: {orderLine.gtin}");
                }
                gtins.Add(orderLine.gtin);
            }

            IEnumerable<ProductDataModel> productDataModels = _productRepository.GetProductsByGtin(gtins);
            Dictionary<string, Product> products = productDataModels.ToDictionary(p => p.Gtin, p => new Product(p));

            Log.Debug(String.Format("Retrieved products to verify manifest: {0}", products));

            var lineItems = new List<StockAlteration>();
            var errors = new List<string>();

            foreach (var orderLine in requestModel.OrderLines)
            {
                if (!products.ContainsKey(orderLine.gtin))
                {
                    errors.Add(String.Format("Unknown product gtin: {0}", orderLine.gtin));
                    continue;
                }

                Product product = products[orderLine.gtin];
                if (!product.Gcp.Equals(requestModel.Gcp))
                {
                    errors.Add(String.Format("Manifest GCP ({0}) doesn't match Product GCP ({1})",
                        requestModel.Gcp, product.Gcp));
                }
                else
                {
                    lineItems.Add(new StockAlteration(product.Id, orderLine.quantity));
                }
            }

            if (errors.Count() > 0)
            {
                Log.Debug(String.Format("Found errors with inbound manifest: {0}", errors));
                throw new ValidationException(String.Format("Found inconsistencies in the inbound manifest: {0}", String.Join("; ", errors)));
            }

            Log.Debug(String.Format("Increasing stock levels with manifest: {0}", requestModel));
            _stockRepository.AddStock(requestModel.WarehouseId, lineItems);
            Log.Info("Stock levels increased");
        }
    }
}
