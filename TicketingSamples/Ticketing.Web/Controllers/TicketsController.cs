using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ticketing.Models;
using Ticketing.Web;
using Ticketing.Web.Controllers;
using TicketingWeb.Models;

namespace TicketingWeb
{
    public class TicketsController : QueueControllerBase
    {
        private readonly TicketingWebContext _context;

        public TicketsController(TicketingWebContext context, ConnectionStrings connectionStrings) 
            : base(connectionStrings)
        {            
            _context = context;
        }

        // GET: Tickets
        public async Task<IActionResult> Index()
        {
            // Put something in session so we  retain a fixed session key.  It will change with every request otherwise
            HttpContext.Session.Set("key", new byte[1]);
                   

            return View(await _context.Ticket.ToListAsync());
        }

        // GET: Tickets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Ticket
                .SingleOrDefaultAsync(m => m.TicketId == id);
            if (ticket == null)
            {
                return NotFound();
            }

            string response1 = await base.GetResponseStorageQueue();
            string response2 = await base.GetResponseServiceBus();

            ticket.Comments = new List<Comment>();
            ticket.Comments.Add(new Comment { Text = response1 });
            ticket.Comments.Add(new Comment { Text = response2 });
            
            return View(ticket);
        }

        // GET: Tickets/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tickets/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TicketId,TicketType,UpdatedAt,UpdatedBy")] Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                var ticketJson = JsonConvert.SerializeObject(ticket);
                
                _context.Add(ticket);
                await _context.SaveChangesAsync();
                

                // Send the json serialised ticket to the storage queue
                base.SendRequestStorageQueue(ticketJson);

                // Send the json serialised ticket to the service bus queue
                base.SendRequestServiceBus(ticketJson);
        
                return RedirectToAction(nameof(Index));
            }
            return View(ticket);
        }

        // GET: Tickets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Ticket.SingleOrDefaultAsync(m => m.TicketId == id);
            if (ticket == null)
            {
                return NotFound();
            }
            return View(ticket);
        }

        // POST: Tickets/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TicketId,TicketType,UpdatedAt,UpdatedBy")] Ticket ticket)
        {
            if (id != ticket.TicketId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ticket);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TicketExists(ticket.TicketId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(ticket);
        }

        // GET: Tickets/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Ticket
                .SingleOrDefaultAsync(m => m.TicketId == id);
            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        // POST: Tickets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ticket = await _context.Ticket.SingleOrDefaultAsync(m => m.TicketId == id);
            _context.Ticket.Remove(ticket);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TicketExists(int id)
        {
            return _context.Ticket.Any(e => e.TicketId == id);
        }
    }
}
