using System.ComponentModel.DataAnnotations;

namespace Api.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long Price { get; set; }
    }
}
