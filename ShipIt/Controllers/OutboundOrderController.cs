﻿using System;
using System.Collections.Generic;
using System.Linq;
 using Microsoft.AspNetCore.Mvc;
 using NpgsqlTypes;
 using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Repositories;
 using static ShipIt_DotNetCore.Helpers.OutboundOrdersHelper;


namespace ShipIt.Controllers
{
    [Route("orders/outbound")]
    public class OutboundOrderController : ControllerBase
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);

        private readonly IStockRepository _stockRepository;
        private readonly IProductRepository _productRepository;

        public OutboundOrderController(IStockRepository stockRepository, IProductRepository productRepository)
        {
            _stockRepository = stockRepository;
            _productRepository = productRepository;
        }

        [HttpPost("")]
        public OutboundOrderResponse ProcessOutboundOrder([FromBody] OutboundOrderRequestModel request)
        {
            Log.Info($"Processing outbound order: {request}");

            var gtins = new List<String>();
            foreach (var orderLine in request.OrderLines)
            {
                if (gtins.Contains(orderLine.gtin))
                {
                    throw new ValidationException(
                        $"Outbound order request contains duplicate product gtin: {orderLine.gtin}");
                }
                gtins.Add(orderLine.gtin);
            }
            
            var productDataModels = _productRepository.GetProductsByGtin(gtins);
            var products = productDataModels.ToDictionary(p => p.Gtin, p => new Product(p));
            var lineItems = new List<StockAlteration>();
            var trucksForCurrentOrder = new Dictionary<int, List<Product>>();
            var currentTruckProducts = new List<Product>();
            var productIds = new List<int>();
            var errors = new List<string>();


            var totalAmountOfTrucks = trucksForCurrentOrder.Count;

            float currentTruckWeight = 0;
            
            foreach (var orderLine in request.OrderLines)
            {
                if (!products.ContainsKey(orderLine.gtin))
                {
                    errors.Add($"Unknown product gtin: {orderLine.gtin}");
                }
                var product = products[orderLine.gtin];
                
                
                if ((currentTruckWeight + product.Weight * orderLine.quantity) < 2000)
                {
                    currentTruckWeight += product.Weight * orderLine.quantity;
                    currentTruckProducts.Add(product);
                }
                trucksForCurrentOrder.Add(trucksForCurrentOrder.Count + 1, currentTruckProducts);
                currentTruckProducts.Clear();
                
                lineItems.Add(new StockAlteration(product.Id, orderLine.quantity));
                productIds.Add(product.Id);
            }

            if (errors.Count > 0)
            {
                throw new NoSuchEntityException(string.Join("; ", errors));
            }

            var stock = _stockRepository.GetStockByWarehouseAndProductIds(request.WarehouseId, productIds);

            var orderLines = request.OrderLines.ToList();
            errors = new List<string>();
            

            for (int i = 0; i < lineItems.Count; i++)
            {
                var lineItem = lineItems[i];
                var orderLine = orderLines[i];

                if (!stock.ContainsKey(lineItem.ProductId))
                {
                    errors.Add($"Product: {orderLine.gtin}, no stock held");
                    continue;
                }

                var item = stock[lineItem.ProductId];
                if (lineItem.Quantity > item.held)
                {
                    errors.Add(
                        $"Product: {orderLine.gtin}, stock held: {item.held}, stock to remove: {lineItem.Quantity}");
                }
            }

            if (errors.Count > 0)
            {
                throw new InsufficientStockException(string.Join("; ", errors));
            }

            _stockRepository.RemoveStock(request.WarehouseId, lineItems);

            return new OutboundOrderResponse {TotalTrucks = totalAmountOfTrucks};
        }
        
    }
}