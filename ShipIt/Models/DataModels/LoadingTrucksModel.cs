using System.Data;

namespace ShipIt.Models.DataModels
{
    public class LoadingTrucksModel : DataModel
    {
        [DatabaseColumnName("p_id")]
        public int ProductId { get; set; }
        
        [DatabaseColumnName("w_id")]
        public int WarehouseId { get; set; }
        
        [DatabaseColumnName("hld")]
        public int Held { get; set; }
        
        [DatabaseColumnName("gtin_cd")]
        public string Gtin { get; set; }

        [DatabaseColumnName("gcp_cd")]
        public string Gcp { get; set; }
        
        [DatabaseColumnName("gln_nm")]
        public string GcpName { get; set; }

        [DatabaseColumnName("gtin_nm")]
        public string GtinName { get; set; }

        [DatabaseColumnName("m_g")]
        public double Weight { get; set; }

        [DatabaseColumnName("l_th")]
        public int LowerThreshold { get; set; }

        [DatabaseColumnName("ds")]
        public int Discontinued { get; set; }

        [DatabaseColumnName("min_qt")]
        public int MinimumOrderQuantity { get; set; }
        
        public LoadingTrucksModel(IDataReader dataReader): base(dataReader) { }
        
        public LoadingTrucksModel() {}
    }
}