namespace Contract_Monthly_Claim_System.Models
{
    public class SupportingDocument
    {
        public string Id { get; set; }
        public string ClaimId { get; set; }
        public string FileName { get; set; }
        public byte[] EncryptedContent { get; set; }
    }
}