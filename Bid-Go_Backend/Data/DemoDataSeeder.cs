using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bid_Go_Backend.Data
{
    /// <summary>
    /// Populates the database with a small, self-consistent demo dataset: one company, three
    /// drivers, three transport requests and some bids already placed against them.
    ///
    /// This exists only so the public demo has something to show. It never runs unless
    /// SEED_DEMO_DATA=true, so a local or real deployment is untouched.
    ///
    /// Dates are relative to "now" on every run, so a reseed always produces auctions that are
    /// currently open rather than ones that expired whenever the seed was written.
    /// </summary>
    public static class DemoDataSeeder
    {
        public const string DemoPassword = "demo1234";

        private const string CompanyEmail = "demo.company@bidgo.app";
        private const string DriverEmail = "demo.driver@bidgo.app";

        /// <summary>
        /// Seeds demo data. Idempotent: does nothing if the demo company already exists,
        /// unless <paramref name="force"/> is set, which wipes the existing data first.
        /// </summary>
        public static async Task SeedAsync(BidGoDbContext db, bool force = false)
        {
            var alreadySeeded = await db.Users.AnyAsync(u => u.Email == CompanyEmail);

            if (alreadySeeded && !force)
            {
                Console.WriteLine("Demo data already present; skipping seed.");
                return;
            }

            if (force)
            {
                await WipeAsync(db);
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(DemoPassword);

            var company = new Company
            {
                Name = "Ana Ribeiro",
                CompanyName = "Ribeiro Logística Lda.",
                Email = CompanyEmail,
                Password = hashedPassword,
                PhoneNumber = 912345678,
                NIF = 501234567,
                Address = "Rua de Santa Catarina 145, Porto",
                IsActive = true
            };

            // The first driver is the one whose credentials are published on the demo login page.
            // The other two exist so the auctions have competing bids rather than a single offer.
            var driver = NewDriver("Miguel Santos", DriverEmail, 913222444, 234567891, hashedPassword);
            var rival1 = NewDriver("Carla Nunes", "carla.nunes@bidgo.app", 914333555, 234567892, hashedPassword);
            var rival2 = NewDriver("Rui Faria", "rui.faria@bidgo.app", 915444666, 234567893, hashedPassword);

            db.Companies.Add(company);
            db.Drivers.AddRange(driver, rival1, rival2);
            await db.SaveChangesAsync();

            var today = DateTime.UtcNow.Date;

            // Automatic selection ON: the background service resolves the winner when bidding closes.
            var fridge = NewRequest(
                company.Id,
                origin: "Rua de Santa Catarina 145, Porto, 4000-447",
                destination: "Avenida da Liberdade 62, Lisboa, 1250-145",
                package: "Household appliance (fridge)",
                weight: 85m, length: 70m, width: 65m, height: 180m,
                pickup: today.AddDays(7), delivery: today.AddDays(8),
                biddingStart: today.AddDays(-2), biddingEnd: today.AddDays(5),
                maxPrice: 240.00m,
                automatic: true);

            // Automatic selection OFF: the company accepts or rejects each bid by hand.
            var pallet = NewRequest(
                company.Id,
                origin: "Zona Industrial da Maia, Rua C Lote 12, Maia, 4470-177",
                destination: "Parque Industrial de Coimbrões, Viseu, 3500-618",
                package: "Palletised goods (bottled beverages)",
                weight: 420m, length: 120m, width: 100m, height: 150m,
                pickup: today.AddDays(10), delivery: today.AddDays(11),
                biddingStart: today.AddDays(-1), biddingEnd: today.AddDays(7),
                maxPrice: 380.00m,
                automatic: false);

            // No bids yet, so a visitor logged in as the driver has something to bid on themselves.
            var sofa = NewRequest(
                company.Id,
                origin: "Rua Cândido dos Reis 88, Faro, 8000-141",
                destination: "Praça 5 de Outubro 3, Portimão, 8500-540",
                package: "Furniture (two-seat sofa)",
                weight: 45m, length: 180m, width: 90m, height: 85m,
                pickup: today.AddDays(5), delivery: today.AddDays(6),
                biddingStart: today, biddingEnd: today.AddDays(3),
                maxPrice: 120.00m,
                automatic: true);

            db.TransportRequests.AddRange(fridge, pallet, sofa);
            await db.SaveChangesAsync();

            db.Bids.AddRange(
                NewBid(rival1.Id, fridge.TransportRequestId, 215.00m, fridge.DeliveryDate),
                NewBid(rival2.Id, fridge.TransportRequestId, 228.50m, fridge.DeliveryDate),
                NewBid(driver.Id, fridge.TransportRequestId, 199.00m, fridge.DeliveryDate),
                NewBid(rival1.Id, pallet.TransportRequestId, 350.00m, pallet.DeliveryDate),
                NewBid(rival2.Id, pallet.TransportRequestId, 372.00m, pallet.DeliveryDate)
            );

            await db.SaveChangesAsync();

            Console.WriteLine($"Demo data seeded: 1 company, 3 drivers, 3 transport requests, 5 bids. Login: {CompanyEmail} / {DriverEmail}, password '{DemoPassword}'.");
        }

        /// <summary>
        /// Clears the transactional tables. Order matters: dependants first, because most of the
        /// foreign keys are configured with DeleteBehavior.Restrict rather than cascade.
        /// </summary>
        private static async Task WipeAsync(BidGoDbContext db)
        {
            db.Messages.RemoveRange(await db.Messages.ToListAsync());
            db.Chats.RemoveRange(await db.Chats.ToListAsync());
            db.Notifications.RemoveRange(await db.Notifications.ToListAsync());
            db.Payments.RemoveRange(await db.Payments.ToListAsync());
            db.Reviews.RemoveRange(await db.Reviews.ToListAsync());
            await db.SaveChangesAsync();

            // A transport request points at its winning bid, so that reference has to go before
            // the bids themselves can be deleted.
            foreach (var request in await db.TransportRequests.ToListAsync())
            {
                request.SelectedBidId = null;
            }
            await db.SaveChangesAsync();

            db.Bids.RemoveRange(await db.Bids.ToListAsync());
            await db.SaveChangesAsync();

            db.TransportRequests.RemoveRange(await db.TransportRequests.ToListAsync());
            await db.SaveChangesAsync();

            db.Users.RemoveRange(await db.Users.ToListAsync());
            await db.SaveChangesAsync();

            Console.WriteLine("Existing data wiped before reseed.");
        }

        private static Driver NewDriver(string name, string email, int phone, int nif, string password) => new()
        {
            Name = name,
            Email = email,
            Password = password,
            PhoneNumber = phone,
            NIF = nif,
            IsActive = true,
            // Both are image URLs in normal use. Empty is the same value the app stores when
            // Cloudflare R2 is not configured, so nothing downstream has to special-case it.
            DriverLicense = string.Empty,
            Insurance = string.Empty
        };

        private static TransportRequest NewRequest(
            int companyId,
            string origin,
            string destination,
            string package,
            decimal weight,
            decimal length,
            decimal width,
            decimal height,
            DateTime pickup,
            DateTime delivery,
            DateTime biddingStart,
            DateTime biddingEnd,
            decimal maxPrice,
            bool automatic) => new()
            {
                CompanyId = companyId,
                Origin = origin,
                Destination = destination,
                Package = package,
                Weight = weight,
                Length = length,
                Width = width,
                Height = height,
                Volume = decimal.Round(length * width * height / 1_000_000m, 2), // cm³ -> m³
                PickupDate = pickup,
                DeliveryDate = delivery,
                BiddingStartDate = biddingStart,
                BiddingEndDate = biddingEnd,
                MaxPrice = maxPrice,
                Status = ERequestStatus.Active,
                IsAutomaticSelectionEnabled = automatic,
                IsAutomaticSelectionExecuted = false,
                Image = string.Empty
            };

        private static Bid NewBid(int driverId, int transportRequestId, decimal value, DateTime deadline) => new()
        {
            DriverId = driverId,
            TransportRequestId = transportRequestId,
            Value = value,
            DeliveryDeadline = deadline,
            Status = EBidStatus.Pendent
        };
    }
}
