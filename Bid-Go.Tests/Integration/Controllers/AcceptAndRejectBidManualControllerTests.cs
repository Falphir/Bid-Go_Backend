using Bid_Go.Tests.Integration.Utils;
using Bid_Go_Backend.Controllers;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Services.Bids;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Xunit;

namespace Bid_Go.Tests.Integration.Controllers
{
 public class AcceptAndRejectBidManualControllerTests
 {
 private readonly AcceptAndRejectBidManualController _controller;
 private readonly FakeAcceptAndRejectBidManualRepository _repo;
 private readonly FakeAuthorizationService _authz;
 private readonly FakeNotificationService _notifications;

 public AcceptAndRejectBidManualControllerTests()
 {
 _repo = new FakeAcceptAndRejectBidManualRepository();
 _authz = new FakeAuthorizationService();
 _notifications = new FakeNotificationService();
 var service = new AcceptAndRejectBidManualService(_repo, _notifications);
 _controller = new AcceptAndRejectBidManualController(service, _authz);

 // add fake user identity with userId claim (company)
 var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
 {
 new Claim("userId", "10"),
 new Claim(ClaimTypes.Role, "Company")
 }, "TestAuth"));
 _controller.ControllerContext = new ControllerContext
 {
 HttpContext = new DefaultHttpContext { User = user }
 };
 }

 private (TransportRequest req, Bid accepted, Bid pending, Bid otherPending) SeedRequestWithBids()
 {
 var tr = _repo.AddRequest(new TransportRequest
 {
 CompanyId =10,
 Status = ERequestStatus.Active,
 Origin = "A",
 Destination = "B",
 Package = "Box",
 Weight =1,
 Volume =1,
 Length =1,
 Width =1,
 Height =1,
 PickupDate = DateTime.UtcNow,
 DeliveryDate = DateTime.UtcNow.AddDays(1),
 Image = "img",
 MaxPrice =100,
 BiddingStartDate = DateTime.UtcNow.AddDays(-1),
 BiddingEndDate = DateTime.UtcNow.AddDays(1),
 IsAutomaticSelectionEnabled = false
 });
 var b1 = _repo.AddBid(new Bid { DriverId =1, TransportRequestId = tr.TransportRequestId, Status = EBidStatus.Pendent, Value =10, DeliveryDeadline = DateTime.UtcNow.AddDays(1) });
 var b2 = _repo.AddBid(new Bid { DriverId =2, TransportRequestId = tr.TransportRequestId, Status = EBidStatus.Pendent, Value =12, DeliveryDeadline = DateTime.UtcNow.AddDays(1) });
 var b3 = _repo.AddBid(new Bid { DriverId =3, TransportRequestId = tr.TransportRequestId, Status = EBidStatus.Pendent, Value =15, DeliveryDeadline = DateTime.UtcNow.AddDays(1) });
 return (tr, b1, b2, b3);
 }

 [Fact]
 public async Task GetBidsByRequest_ReturnsOk_WithList()
 {
 var (tr, b1, b2, b3) = SeedRequestWithBids();
 var result = await _controller.GetBidsByTransportRequest(tr.TransportRequestId);
 var ok = Assert.IsType<OkObjectResult>(result);
 var list = Assert.IsType<List<Bid>>(ok.Value);
 Assert.Equal(3, list.Count);
 }

 [Fact]
 public async Task GetBidsByRequestAndStatus_ReturnsOk_WithFilteredList()
 {
 var (tr, b1, b2, b3) = SeedRequestWithBids();
 // set one to Accepted to test filter
 b2.Status = EBidStatus.Accepted;
 var result = await _controller.GetBidsByTransportRequestAndStatus(tr.TransportRequestId, EBidStatus.Accepted);
 var ok = Assert.IsType<OkObjectResult>(result);
 var list = Assert.IsType<List<Bid>>(ok.Value);
 Assert.Single(list);
 Assert.Equal(b2.BidId, list[0].BidId);
 }

 [Fact]
 public async Task AcceptBid_ChangesStatuses_SendsNotifications()
 {
 var (tr, target, other1, other2) = SeedRequestWithBids();
 var result = await _controller.AcceptBid(target.BidId);
 var ok = Assert.IsType<OkObjectResult>(result);
 Assert.Equal(200, ok.StatusCode);

 // repository state
 var req = _repo.GetRequest(tr.TransportRequestId)!;
 Assert.Equal(ERequestStatus.Pending, req.Status);
 Assert.Equal(target.BidId, req.SelectedBidId);

 var bids = await _repo.GetByTransportRequestAsync(tr.TransportRequestId);
 Assert.Contains(bids, b => b.BidId == target.BidId && b.Status == EBidStatus.Accepted);
 Assert.Equal(2, bids.Count(b => b.Status == EBidStatus.Rejected));

 // notifications
 Assert.Contains(_notifications.Created, n => n.Type == ENotificationType.Accepted && n.BidId == target.BidId);
 Assert.Equal(2, _notifications.Created.Count(n => n.Type == ENotificationType.Rejected && n.TransportRequestId == tr.TransportRequestId));
 }

 [Fact]
 public async Task RejectBid_ChangesStatus_SendsNotification()
 {
 var (tr, target, other1, other2) = SeedRequestWithBids();
 var result = await _controller.RejectBid(target.BidId);
 var ok = Assert.IsType<OkObjectResult>(result);
 Assert.Equal(200, ok.StatusCode);

 var b = await _repo.GetByIdAsync(target.BidId);
 Assert.Equal(EBidStatus.Rejected, b!.Status);
 Assert.Contains(_notifications.Created, n => n.Type == ENotificationType.Rejected && n.BidId == target.BidId);
 }
 }
}
