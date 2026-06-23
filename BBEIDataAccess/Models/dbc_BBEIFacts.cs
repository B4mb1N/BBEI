using System.ComponentModel.DataAnnotations;
namespace BBEIDataAccess.Models
{
    public partial class dbc_BBEIFacts
    {
        [Key]
        public long Id { get; set; }
        public string Question { get; set; }
        public string Reply { get; set; }
        public long IdAuthor { get; set; }
        public DateTime DateFact { get; set; }
    }
}
