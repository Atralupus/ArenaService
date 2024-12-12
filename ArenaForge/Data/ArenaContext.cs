using Microsoft.EntityFrameworkCore;
using ArenaForge.Models;

namespace ArenaForge.Data
{
    public class ArenaContext : DbContext
    {
        public ArenaContext(DbContextOptions<ArenaContext> options) : base(options) { }

        public DbSet<Participant> Participants { get; set; }
    }
} 