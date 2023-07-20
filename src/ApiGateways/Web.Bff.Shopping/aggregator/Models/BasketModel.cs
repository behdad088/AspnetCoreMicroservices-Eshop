namespace Web.Shopping.HttpAggregator.Models
{
    public class BasketModel
    {
        public string Username { get; set; }
        public List<BasketItemExtendedModel> Items { get; set; } = new List<BasketItemExtendedModel>();
        public decimal TotalPrice { get; set; }
    }
}
