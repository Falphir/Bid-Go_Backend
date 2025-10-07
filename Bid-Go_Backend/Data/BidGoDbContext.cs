using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;

namespace Bid_Go_Backend.Data2
{

    public class BidGoDbContext : DbContext
    {
        public BidGoDbContext(DbContextOptions<BidGoDbContext> options) : base(options)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Bid> Bids { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ReviewCompany> ReviewCompanies { get; set; }
        public DbSet<ReviewDriver> ReviewDrivers { get; set; }
        public DbSet<TransportRequest> TransportRequests { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var stringEnumConverter = new ValueConverter<Enum, string>(
                v => v.ToString(),
                v => (Enum)Enum.Parse(typeof(Enum), v));

            var cargoEnumConverter = new ValueConverter<EBidStatus, string>(
                v => v.ToString(),
                v => (EBidStatus)Enum.Parse(typeof(EBidStatus), v));

            var candidaturaEstadoEnumConverter = new ValueConverter<EChatStatus, string>(
                v => v.ToString(),
                v => (EChatStatus)Enum.Parse(typeof(EChatStatus), v));

            var campanhaEstadoEnumConverter = new ValueConverter<ENotificationType, string>(
                v => v.ToString(),
                v => (ENotificationType)Enum.Parse(typeof(ENotificationType), v));

            var campanhaTipoEnumConverter = new ValueConverter<EPaymentStatus, string>(
                v => v.ToString(),
                v => (EPaymentStatus)Enum.Parse(typeof(EPaymentStatus), v));

            var encomendaEstadoEnumConverter = new ValueConverter<ERequestStatus, string>(
                v => v.ToString(),
                v => (ERequestStatus)Enum.Parse(typeof(ERequestStatus), v));

            modelBuilder.Entity<Utilizador>()
                .HasDiscriminator<ECargo>("Cargo")
                .HasValue<Utilizador>(ECargo.Voluntario)
                .HasValue<Anfitriao>(ECargo.Anfitriao)
                .HasValue<Administrador>(ECargo.Admin);

            modelBuilder.Entity<Utilizador>()
                .Property(p => p.Cargo)
                .HasConversion(cargoEnumConverter);

            modelBuilder.Entity<CandidaturaAnfitriao>()
                .Property(p => p.Estado)
                .HasConversion(candidaturaEstadoEnumConverter);
            
            modelBuilder.Entity<Campanha>()
                .Property(p => p.Estado)
                .HasConversion(campanhaEstadoEnumConverter);

            modelBuilder.Entity<Campanha>()
                .Property(p => p.TipoCampanha)
                .HasConversion(campanhaTipoEnumConverter);

            modelBuilder.Entity<Encomenda>()
                .Property(p => p.Estado)
                .HasConversion(encomendaEstadoEnumConverter);

            modelBuilder.Entity<Produto>()
                .Property(p => p.Tamanho)
                .HasConversion(produtoTamanhoEnumConverter);

            modelBuilder.Entity<Ticket>()
                .Property(p => p.Estado)
                .HasConversion(ticketEstadoEnumConverter);

            modelBuilder.Entity<Ticket>()
                .Property(p => p.Prioridade)
                .HasDefaultValue(false);

            modelBuilder.Entity<Utilizador>()
                .Property(p => p.Membership)
                .HasDefaultValue(0);

            modelBuilder.Entity<Utilizador>()
                .HasIndex(p => p.Email)
                .IsUnique();

            modelBuilder.Entity<Doacao>()
                .Property(p => p.Valor)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Produto>()
                .Property(p => p.Preco)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Encomenda>()
                .Property(p => p.ValorTotal)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Campanha>()
                .Property(p => p.QuantiaAngariada)
                .HasPrecision(10, 2);
        }
    }
}