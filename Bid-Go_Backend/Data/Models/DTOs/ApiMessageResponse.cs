namespace Bid_Go_Backend.Data.Models.DTOs
{
 public class ApiMessageResponse<T>
 {
 public string message { get; set; } = string.Empty;
 public T payment { get; set; } = default!;
 }
}
