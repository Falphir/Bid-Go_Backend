using AjudaMaisEF.Data.Models.Enums;
using Bid_Go_Backend.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;

namespace AjudaMaisEF.Data.Models
{

    public class AjudaMaisContext : DbContext
    {
        public AjudaMaisContext(DbContextOptions<AjudaMaisContext> options) : base(options)
        {

        }

        public DbSet<User> User { get; set; }
        public DbSet<Company> Company { get; set; }
        public DbSet<Driver> Driver { get; set; }
        public DbSet<Licit> Doacoes { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<Encomenda> Encomendas { get; set; }
        public DbSet<LinhasVenda> LinhasVenda { get; set; }
        public DbSet<Carrinho> Carrinhos { get; set; }
        public DbSet<CarrinhoItem> CarrinhoItems { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Anexo> Anexos { get; set; }
        public DbSet<Workshop> Workshops { get; set; }
        public DbSet<Mensagem> Mensagens { get; set; }
        public DbSet<CandidaturaAnfitriao> CandidaturaAnfitriao { get; set; }
        public DbSet<Notificacao> Notificacao { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var stringEnumConverter = new ValueConverter<Enum, string>(
                v => v.ToString(),
                v => (Enum)Enum.Parse(typeof(Enum), v));

            var cargoEnumConverter = new ValueConverter<ECargo, string>(
                v => v.ToString(),
                v => (ECargo)Enum.Parse(typeof(ECargo), v));

            var candidaturaEstadoEnumConverter = new ValueConverter<ECandidaturaEstado, string>(
                v => v.ToString(),
                v => (ECandidaturaEstado)Enum.Parse(typeof(ECandidaturaEstado), v));

            var campanhaEstadoEnumConverter = new ValueConverter<ECampanhaEstado, string>(
                v => v.ToString(),
                v => (ECampanhaEstado)Enum.Parse(typeof(ECampanhaEstado), v));

            var campanhaTipoEnumConverter = new ValueConverter<ECampanhaTipo, string>(
                v => v.ToString(),
                v => (ECampanhaTipo)Enum.Parse(typeof(ECampanhaTipo), v));

            var encomendaEstadoEnumConverter = new ValueConverter<EEncomendaEstado, string>(
                v => v.ToString(),
                v => (EEncomendaEstado)Enum.Parse(typeof(EEncomendaEstado), v));

            var produtoTamanhoEnumConverter = new ValueConverter<EProdutoTamanho, string>(
                v => v.ToString(),
                v => (EProdutoTamanho)Enum.Parse(typeof(EProdutoTamanho), v));

            var ticketEstadoEnumConverter = new ValueConverter<ETicketEstado, string>(
                v => v.ToString(),
                v => (ETicketEstado)Enum.Parse(typeof(ETicketEstado), v));

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