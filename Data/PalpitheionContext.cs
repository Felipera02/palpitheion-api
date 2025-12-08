using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PalpitheionApi.Models;

namespace PalpitheionApi.Data;

public class PalpitheionContext(
    DbContextOptions<PalpitheionContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Indicado> Indicados { get; set; }
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<Palpite> Palpites { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Categoria>()
            .HasMany(c => c.Indicados)
            .WithMany(i => i.Categorias);

        builder.Entity<Categoria>()
            .HasOne(c => c.IndicadoVencedor)
            .WithMany()
            .HasForeignKey(c => c.IndicadoVencedorId);
    }
}
