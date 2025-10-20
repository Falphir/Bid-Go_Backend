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

            // Configuração discriminator para Reviews
            modelBuilder.Entity<Review>()
               .HasDiscriminator<string>("Discriminator")
               .HasValue<ReviewCompany>("Driver")
               .HasValue<ReviewDriver>("Company");

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

            // TransportRequest -> Chat (1-to-1)
            modelBuilder.Entity<TransportRequest>()
                .HasOne(t => t.Chat)
                .WithOne(c => c.TransportRequest)
                .HasForeignKey<Chat>(c => c.TransportRequestId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Chat>()
                .HasOne(t => t.TransportRequest)
                .WithOne(c => c.Chat)
                .HasForeignKey<Chat>(c => c.TransportRequestId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // Chat -> Messages (1-to-many)
            modelBuilder.Entity<Chat>()
                .HasMany(c => c.Messages)
                .WithOne(m => m.Chat)
                .HasForeignKey(m => m.ChatId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            //Configuração 1:1 entre TransportRequest e Payment
            modelBuilder.Entity<TransportRequest>()
            .HasOne(tr => tr.Payment) // TransportRequest TEM UM Payment
            .WithOne(p => p.TransportRequest) // Payment TEM UM TransportRequest
            .HasForeignKey<Payment>(p => p.TransportRequestId) 
            .IsRequired() // obrigatório
            .OnDelete(DeleteBehavior.Cascade);

            //TransportRequest 1:N Reviews
            modelBuilder.Entity<TransportRequest>()
             .HasMany(tr => tr.Reviews)
             .WithOne(r => r.TransportRequest)
             .HasForeignKey(r => r.TransportRequestId)
             .IsRequired()
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}