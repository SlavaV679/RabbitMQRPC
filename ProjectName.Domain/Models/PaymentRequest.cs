namespace ProjectName.Domain.Models
{
    public class PaymentRequest
    {
        public int ActId { get; set; }

        public DateTime PaymentDate { get; set; }

        public long Summa { get; set; }

        public string Currency { get; set; }

        public string Notes { get; set; }

        public string Inn { get; set; }

        public int? FailedRequestId { get; set; }
    }
}
