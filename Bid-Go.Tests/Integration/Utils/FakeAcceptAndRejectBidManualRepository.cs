using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.Interfaces;
using System.Collections.Concurrent;

namespace Bid_Go.Tests.Integration.Utils
{
 public class FakeAcceptAndRejectBidManualRepository : IAcceptAndRejectBidManualRepository
 {
 private readonly ConcurrentDictionary<int, TransportRequest> _requests = new();
 private readonly ConcurrentDictionary<int, Bid> _bids = new();
 private int _nextBidId =1;
 private int _nextRequestId =1;

 public TransportRequest AddRequest(TransportRequest request)
 {
 request.TransportRequestId = _nextRequestId++;
 _requests[request.TransportRequestId] = request;
 return request;
 }

 public Bid AddBid(Bid bid)
 {
 bid.BidId = _nextBidId++;
 _bids[bid.BidId] = bid;
 return bid;
 }

 public Task<Bid?> GetByIdAsync(int id)
 {
 _bids.TryGetValue(id, out var bid);
 if (bid != null && bid.TransportRequest == null)
 {
 if (_requests.TryGetValue(bid.TransportRequestId, out var tr))
 {
 bid.TransportRequest = tr;
 }
 }
 return Task.FromResult(bid);
 }

 public Task<List<Bid>> GetByTransportRequestAsync(int transportRequestId)
 {
 var list = _bids.Values.Where(b => b.TransportRequestId == transportRequestId).ToList();
 foreach (var b in list)
 {
 if (b.TransportRequest == null && _requests.TryGetValue(b.TransportRequestId, out var tr))
 b.TransportRequest = tr;
 }
 return Task.FromResult(list);
 }

 public Task<List<Bid>> GetByTransportRequestAndStatusAsync(int transportRequestId, EBidStatus status)
 {
 var list = _bids.Values.Where(b => b.TransportRequestId == transportRequestId && b.Status == status).ToList();
 foreach (var b in list)
 {
 if (b.TransportRequest == null && _requests.TryGetValue(b.TransportRequestId, out var tr))
 b.TransportRequest = tr;
 }
 return Task.FromResult(list);
 }

 public Task UpdateAsync(Bid bid)
 {
 _bids[bid.BidId] = bid;
 return Task.CompletedTask;
 }

 public Task SaveChangesAsync() => Task.CompletedTask;

 public TransportRequest? GetRequest(int id) => _requests.TryGetValue(id, out var tr) ? tr : null;
 }
}
