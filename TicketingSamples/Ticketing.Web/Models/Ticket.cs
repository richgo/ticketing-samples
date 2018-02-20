using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ticketing.Models
{
    public class Ticket
    {
        public int TicketId { get; set; }
        public eTicketType TicketType { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public IList<Comment> Comments { get; set; }

        public enum eTicketType
        {
            Incident,
            Request
        }

        public Ticket()
        {
            UpdatedAt = DateTime.Now;
        }
    }
}
