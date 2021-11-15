using System.Data;

namespace ShipIt.Models.DataModels
{
    public class InboundQueryModel : DataModel
    {
        [DatabaseColumnName("p_id")]
        public int ProductId { get; set; }
        
        [DatabaseColumnName("w_id")]
        public int WarehouseId { get; set; }
        
        [DatabaseColumnName("hld")]
        public int held { get; set; }
        
        [DatabaseColumnName("p_id")]
        public int Id { get; set; }

        [DatabaseColumnName("gtin_cd")]
        public string Gtin { get; set; }

        [DatabaseColumnName("gcp_cd")]
        public string Gcp { get; set; }

        [DatabaseColumnName("gtin_nm")]
        public string Name { get; set; }

        [DatabaseColumnName("m_g")]
        public double Weight { get; set; }

        [DatabaseColumnName("l_th")]
        public int LowerThreshold { get; set; }

        [DatabaseColumnName("ds")]
        public int Discontinued { get; set; }

        [DatabaseColumnName("min_qt")]
        public int MinimumOrderQuantity { get; set; }
        
        [DatabaseColumnName("gln_addr_02")]
        public string Addr2 { get; set; }
        
        [DatabaseColumnName("gln_addr_03")]
        public string Addr3 { get; set; }
        
        [DatabaseColumnName("gln_addr_04")]
        public string Addr4 { get; set; }
        
        [DatabaseColumnName("gln_addr_postalcode")]
        public string PostalCode { get; set; }
        
        [DatabaseColumnName("gln_addr_city")]
        public string City { get; set; }
        
        [DatabaseColumnName("contact_tel")]
        public string Tel { get; set; }
        
        [DatabaseColumnName("contact_mail")]
        public string Mail { get; set; }
        
        public InboundQueryModel(IDataReader dataReader): base(dataReader) { }
        
        public InboundQueryModel() {}

    }
}