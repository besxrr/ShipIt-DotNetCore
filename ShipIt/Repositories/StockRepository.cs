﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Npgsql;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Models.DataModels;

namespace ShipIt.Repositories
{
    public interface IStockRepository
    {
        int GetTrackedItemsCount();
        int GetStockHeldSum();
        IEnumerable<StockDataModel> GetStockByWarehouseId(int id);
        public IEnumerable<InboundQueryModel> QueryIncomingByWarehouseId(int id);
        Dictionary<int, StockDataModel> GetStockByWarehouseAndProductIds(int warehouseId, List<int> productIds);
        void RemoveStock(int warehouseId, List<StockAlteration> lineItems);
        void AddStock(int warehouseId, List<StockAlteration> lineItems);
    }

    public class StockRepository : RepositoryBase, IStockRepository
    {

        public int GetTrackedItemsCount()
        {
            string sql = "SELECT COUNT(*) FROM stock";
            return (int)QueryForLong(sql);
        }

        public int GetStockHeldSum()
        {
            string sql = "SELECT SUM(hld) FROM stock";
            return (int)QueryForLong(sql);
        }

        public IEnumerable<StockDataModel> GetStockByWarehouseId(int id)
        {
            string sql = "SELECT * FROM stock WHERE w_id = @w_id";
            var parameter = new NpgsqlParameter("@w_id", id);
            string noProductWithIdErrorMessage = $"No stock found with w_id: {id}";
            try
            {
                return base.RunGetQuery(sql, reader => new StockDataModel(reader), noProductWithIdErrorMessage, parameter).ToList();
            }
            catch (NoSuchEntityException)
            {
                return new List<StockDataModel>();
            }
        }
        
        public IEnumerable<InboundQueryModel> QueryIncomingByWarehouseId(int id)
        {
            string sql = "SELECT stock.w_id, stock.p_id, stock.hld, gtin.gtin_cd, gtin.gtin_nm, gtin.m_g, gtin.l_th, gtin.ds, gtin.min_qt,gcp.gcp_cd, gcp.gln_nm, gcp.gln_addr_02, gcp.gln_addr_03, gcp.gln_addr_04, gcp.gln_addr_postalcode, gcp.gln_addr_city, gcp.contact_tel, gcp.contact_mail FROM gtin JOIN stock ON gtin.p_id = stock.p_id JOIN gcp ON gcp.gcp_cd = gtin.gcp_cd WHERE stock.w_id = @w_id";
            var parameter = new NpgsqlParameter("@w_id", id);
            string noProductWithIdErrorMessage = $"No stock found with w_id: {id}";
            try
            {
                return base.RunGetQuery(sql, reader => new InboundQueryModel(reader), noProductWithIdErrorMessage, parameter).ToList();
            }
            catch (NoSuchEntityException)
            {
                return new List<InboundQueryModel>();
            }
        }

        public Dictionary<int, StockDataModel> GetStockByWarehouseAndProductIds(int warehouseId, List<int> productIds)
        {
            string sql =
                $"SELECT p_id, hld, w_id FROM stock WHERE w_id = @w_id AND p_id IN ({String.Join(",", productIds)})";
            var parameter = new NpgsqlParameter("@w_id", warehouseId);
            string noProductWithIdErrorMessage =
                $"No stock found with w_id: {warehouseId} and p_ids: {String.Join(",", productIds)}";
            var stock = base.RunGetQuery(sql, reader => new StockDataModel(reader), noProductWithIdErrorMessage, parameter);
            return stock.ToDictionary(s => s.ProductId, s => s);
        }
            
        public void AddStock(int warehouseId, List<StockAlteration> lineItems)
        {
            var parametersList = new List<NpgsqlParameter[]>();
            foreach (var orderLine in lineItems)
            {
                parametersList.Add(
                    new NpgsqlParameter[] {
                        new NpgsqlParameter("@p_id", orderLine.ProductId),
                        new NpgsqlParameter("@w_id", warehouseId),
                        new NpgsqlParameter("@hld", orderLine.Quantity)
                    });
            }

            string sql = "INSERT INTO stock (p_id, w_id, hld) VALUES (@p_id, @w_id, @hld) "
                         + "ON CONFLICT (p_id, w_id) DO UPDATE SET hld = stock.hld + EXCLUDED.hld";

            var recordsAffected = new List<int>();
            foreach (var parameters in parametersList)
            {
                 recordsAffected.Add(
                     RunSingleQueryAndReturnRecordsAffected(sql, parameters)
                 );
            }

            string errorMessage = null;

            for (int i = 0; i < recordsAffected.Count; i++)
            {
                if (recordsAffected[i] == 0)
                {
                    errorMessage =
                        $"Product {parametersList[i][0]} in warehouse {warehouseId} was unexpectedly not updated (rows updated returned {recordsAffected[i]})";
                }
            }

            if (errorMessage != null)
            {
                throw new InvalidStateException(errorMessage);
            }
        }

        public void RemoveStock(int warehouseId, List<StockAlteration> lineItems)
        {
            string sql = $"UPDATE stock SET hld = hld - @hld WHERE w_id = {warehouseId} AND p_id = @p_id";

            var parametersList = new List<NpgsqlParameter[]>();
            foreach (var lineItem in lineItems)
            {
                parametersList.Add(new NpgsqlParameter[]
                {
                    new NpgsqlParameter("@hld", lineItem.Quantity),
                    new NpgsqlParameter("@p_id", lineItem.ProductId)
                });
            }

            base.RunTransaction(sql, parametersList);
        }
    }
}