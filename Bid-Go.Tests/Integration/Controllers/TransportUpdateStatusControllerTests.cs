using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Authorization;
using Bid_Go_Backend.Repositories.Transport_Request;
using Bid_Go_Backend.Services;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Xunit;
using System.Text.Json;

namespace Bid_Go.Tests.Integration.Controllers
{
 public class TransportUpdateStatusControllerTests
 {
 private static (TransportUpdateStatusController controller, BidGoDbContext db, TestNotificationService notifications) Build()
 {
 var options = new DbContextOptionsBuilder<BidGoDbContext>()
 .UseInMemoryDatabase(Guid.NewGuid().ToString())
 .Options;

 var db = new BidGoDbContext(options);
 var repo = new TransportUpdateStatusRepository(db);
 var notifications = new TestNotificationService();
 var logger = LoggerFactory.Create(b => b.AddDebug()).CreateLogger<TransportUpdateStatusService>();
 var authRepo = new AuthorizationRepository(db);
 var service = new TransportUpdateStatusService(repo, notifications, logger, authRepo);
 var authz = new AuthorizationService(authRepo);
 var controller = new TransportUpdateStatusController(service, authz);

 return (controller, db, notifications);
 }

 private static (Company company, Driver driver, TransportRequest tr, Bid p1, Bid p2) SeedActiveWithBids(BidGoDbContext db)
 {
 var company = new Company
 {
 Name = "C",
 CompanyName = "CC",
 Address = "A",
 Email = "c@x.com",
 Password = "x",
 PhoneNumber =900000000,
 NIF =111111111
 };

 var driver = new Driver
 {
 Name = "D",
 Email = "d@x.com",
 Password = "x",
 PhoneNumber =911111111,
 NIF =222222222
 };

 db.Companies.Add(company);
 db.Drivers.Add(driver);
 db.SaveChanges();

 var tr = new TransportRequest
 {
 CompanyId = company.Id,
 Status = ERequestStatus.Active,
 Origin = "O",
 Destination = "D",
 Package = "P",
 Weight =1,
 Volume =1,
 Length =1,
 Width =1,
 Height =1,
 PickupDate = DateTime.UtcNow.AddDays(3),
 DeliveryDate = DateTime.UtcNow.AddDays(5),
 BiddingStartDate = DateTime.UtcNow,
 BiddingEndDate = DateTime.UtcNow.AddDays(1),
 Image = "x",
 MaxPrice =100
 };

 db.TransportRequests.Add(tr);
 db.SaveChanges();

 var p1 = new Bid
 {
 TransportRequestId = tr.TransportRequestId,
 DriverId = driver.Id,
 Status = EBidStatus.Pendent,
 Value =50,
 DeliveryDeadline = DateTime.UtcNow.AddDays(4)
 };

 var p2 = new Bid
 {
 TransportRequestId = tr.TransportRequestId,
 DriverId = driver.Id,
 Status = EBidStatus.Pendent,
 Value =60,
 DeliveryDeadline = DateTime.UtcNow.AddDays(4)
 };

 db.Bids.AddRange(p1, p2);
 db.SaveChanges();

 return (company, driver, tr, p1, p2);
 }

 private static void SetUser(ControllerBase controller, int userId, string role)
 {
 var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
 {
 new Claim("userId", userId.ToString()),
 new Claim("userType", role),
 new Claim(ClaimTypes.Role, role)
 }, "TestAuth"));
 controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };
 }

 [Fact]
 public async Task UpdateRequestStatus_Company_Cancels_Request_And_PendingBids()
 {
 var (controller, db, notifications) = Build();
 var (company, driver, tr, p1, p2) = SeedActiveWithBids(db);
 SetUser(controller, company.Id, "Company");

 var dto = new RequestStatusDTO { Status = ERequestStatus.Canceled };
 var result = await controller.UpdateRequestStatus(tr.TransportRequestId, dto);
 var ok = Assert.IsType<ObjectResult>(result);
 Assert.Equal(200, ok.StatusCode);

 var fromDb = await db.TransportRequests.FindAsync(tr.TransportRequestId);
 Assert.Equal(ERequestStatus.Canceled, fromDb!.Status);

 var bids = await db.Bids.Where(b => b.TransportRequestId == tr.TransportRequestId).ToListAsync();
 Assert.All(bids, b => Assert.Equal(EBidStatus.Canceled, b.Status));

 Assert.Equal(2, notifications.Created.Count(n => n.Type == ENotificationType.Canceled && n.TransportRequestId == tr.TransportRequestId));
 }

 [Fact]
 public async Task UpdateRequestStatus_Company_Active_To_Pending()
 {
 var (controller, db, notifications) = Build();
 var (company, driver, tr, p1, p2) = SeedActiveWithBids(db);
 SetUser(controller, company.Id, "Company");

 var dto = new RequestStatusDTO { Status = ERequestStatus.Pending };
 var result = await controller.UpdateRequestStatus(tr.TransportRequestId, dto);
 var ok = Assert.IsType<ObjectResult>(result);
 Assert.Equal(200, ok.StatusCode);

 var fromDb = await db.TransportRequests.FindAsync(tr.TransportRequestId);
 Assert.Equal(ERequestStatus.Pending, fromDb!.Status);
 }

 [Fact]
 public async Task UpdateRequestStatus_Driver_WaitingPickup_To_InTransit()
 {
 var (controller, db, notifications) = Build();
 var (company, driver, tr, p1, p2) = SeedActiveWithBids(db);

 // preparar SelectedBid com o Driver correto
 var accepted = new Bid { DriverId = driver.Id, TransportRequestId = tr.TransportRequestId, Status = EBidStatus.Accepted };
 db.Bids.Add(accepted);
 await db.SaveChangesAsync();
 tr.SelectedBidId = accepted.BidId;
 await db.SaveChangesAsync();

 SetUser(controller, driver.Id, "Driver");

 tr.Status = ERequestStatus.WaitingPickup;
 await db.SaveChangesAsync();

 var dto = new RequestStatusDTO { Status = ERequestStatus.InTransit };
 var result = await controller.UpdateRequestStatus(tr.TransportRequestId, dto);
 var ok = Assert.IsType<ObjectResult>(result);
 Assert.Equal(200, ok.StatusCode);

 var fromDb = await db.TransportRequests.FindAsync(tr.TransportRequestId);
 Assert.Equal(ERequestStatus.InTransit, fromDb!.Status);
 }

 [Fact]
 public async Task CancelRequestStatus_Company_Cancels_Request_And_PendingBids()
 {
 var (controller, db, notifications) = Build();
 var (company, driver, tr, p1, p2) = SeedActiveWithBids(db);
 SetUser(controller, company.Id, "Company");

 var result = await controller.CancelRequestStatus(tr.TransportRequestId);
 var ok = Assert.IsType<OkObjectResult>(result);

 var json = JsonSerializer.Serialize(ok.Value);
 using var doc = JsonDocument.Parse(json);
 var root = doc.RootElement;

 Assert.True(root.TryGetProperty("message", out var msgProp));
 Assert.Equal("Pedido cancelado com sucesso.", msgProp.GetString());

 var fromDb = await db.TransportRequests.FindAsync(tr.TransportRequestId);
 Assert.Equal(ERequestStatus.Canceled, fromDb!.Status);

 var bids = await db.Bids.Where(b => b.TransportRequestId == tr.TransportRequestId).ToListAsync();
 Assert.All(bids, b => Assert.Equal(EBidStatus.Canceled, b.Status));

 Assert.Equal(2, notifications.Created.Count(n => n.Type == ENotificationType.Canceled && n.TransportRequestId == tr.TransportRequestId));
 }

 private sealed class TestNotificationService : INotificationService
 {
 public List<Notification> Created { get; } = new();

 public Task<List<Notification>> GetNotificationsAsync(int userId, ENotificationType? type = null, string order = "desc")
 {
 var q = Created.Where(n => n.UserId == userId);
 if (type.HasValue) q = q.Where(n => n.Type == type);
 return Task.FromResult(q.ToList());
 }

 public Task<Notification> CreateAndSendAsync(int userId, string context, ENotificationType type, int? bidId = null, int? transportRequestId = null)
 {
 var n = new Notification
 {
 NotificationId = Created.Count +1,
 UserId = userId,
 Context = context,
 Type = type,
 BidId = bidId,
 TransportRequestId = transportRequestId,
 TimeStamp = DateTime.UtcNow
 };

 Created.Add(n);
 return Task.FromResult(n);
 }

 public Task SendToMultipleUsersAsync(IEnumerable<int> userIds, string context, ENotificationType type)
 {
 foreach (var id in userIds)
 {
 Created.Add(new Notification
 {
 NotificationId = Created.Count +1,
 UserId = id,
 Context = context,
 Type = type,
 TimeStamp = DateTime.UtcNow
 });
 }

 return Task.CompletedTask;
 }
 }
 }
}
