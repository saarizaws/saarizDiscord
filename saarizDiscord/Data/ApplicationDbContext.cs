using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using saarizDiscord.Models;
using static System.Net.Mime.MediaTypeNames;

namespace saarizDiscord.Data
{
	public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
	{
		public ApplicationDbContext(DbContextOptions options)
			: base(options)
		{

		}
		public DbSet<ApplicationUser> ApplicationUsers { get; set; }
		
	}
}
