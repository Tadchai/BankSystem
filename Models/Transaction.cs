using System;

namespace BankApp.Models
{
public class Transaction
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public int? ReceiverId { get; set; } 
    public decimal Amount { get; set; }
    public string TransactionType { get; set; }
    public DateTime Timestamp { get; set; }
}


}
