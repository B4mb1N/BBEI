using System.ComponentModel.DataAnnotations;
namespace BBEIDataAccess.Models
{
    public partial class dbc_BBEIAuthors
    {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; }
    }
}
