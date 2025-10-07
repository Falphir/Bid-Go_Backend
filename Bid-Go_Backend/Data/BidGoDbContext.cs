using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;

namespace Bid_Go_Backend.Data
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

            // Configuração da herança (TPH)
            modelBuilder.Entity<User>()
                .HasDiscriminator<string>("UserType")
                .HasValue<User>("User")
                .HasValue<Company>("Company")
                .HasValue<Driver>("Driver");

            // Definir propriedades únicas
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Precisões de valores monetários
            modelBuilder.Entity<Bid>()
                .Property(b => b.Value)
                .HasPrecision(18, 2);

            modelBuilder.Entity<TransportRequest>()
                .Property(t => t.Weight)
                .HasPrecision(18, 2);

            modelBuilder.Entity<TransportRequest>()
                .Property(t => t.Volume)
                .HasPrecision(18, 2);

            // Relações e chaves estrangeiras
            modelBuilder.Entity<Bid>()
                .HasOne(b => b.Driver)
                .WithMany()
                .HasForeignKey(b => b.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReviewCompany>()
                .HasOne(r => r.Company)
                .WithMany()
                .HasForeignKey(r => r.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReviewCompany>()
                .HasOne(r => r.Driver)
                .WithMany()
                .HasForeignKey(r => r.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReviewDriver>()
                .HasOne(r => r.Company)
                .WithMany()
                .HasForeignKey(r => r.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReviewDriver>()
                .HasOne(r => r.Driver)
                .WithMany()
                .HasForeignKey(r => r.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransportRequest>()
                .HasOne(t => t.Company)
                .WithMany()
                .HasForeignKey(t => t.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}