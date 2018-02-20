using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ticketing.Models;

namespace TicketingWeb.Models
{
    public class TicketingWebContext : DbContext
    {
        public TicketingWebContext (DbContextOptions<TicketingWebContext> options)
            : base(options)
        {
            
        }

        public DbSet<Ticketing.Models.Ticket> Ticket { get; set; }

        public DbSet<Ticketing.Models.Comment> Comment { get; set; }
    }
}
