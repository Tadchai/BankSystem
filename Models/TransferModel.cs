using System.ComponentModel.DataAnnotations;

namespace BankApp.Models
{
    public class TransferModel
{
    public int SenderId { get; set; }
    [Required]
    public int ReceiverId { get; set; }
    [Required]
    public decimal Amount { get; set; }
}

}
