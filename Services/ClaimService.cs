using Contract_Monthly_Claim_System.Models;
using System.Collections.Generic;
using System.Linq;

namespace Contract_Monthly_Claim_System.Services
{
    public class ClaimService
    {
        private static List<Claim> _claims = new List<Claim>(); // Static for persistence in memory
        private readonly object _lock = new object(); // For thread safety

        public void AddClaim(Claim claim)
        {
            lock (_lock)
            {
                _claims.Add(claim);
            }
        }

        public List<Claim> GetAllClaims()
        {
            lock (_lock)
            {
                return _claims.ToList();
            }
        }

        public List<Claim> GetClaimsByStatus(ClaimStatus status)
        {
            lock (_lock)
            {
                return _claims.Where(c => c.Status == status).ToList();
            }
        }

        public void UpdateStatus(string id, ClaimStatus newStatus)
        {
            lock (_lock)
            {
                var claim = _claims.FirstOrDefault(c => c.Id == id);
                if (claim != null) claim.Status = newStatus;
            }
        }
    }
}