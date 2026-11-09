using BudgetTracker.Core.Models;
namespace BudgetTracker.Core.Services;
public static class TransactionPipeline {
 // Deferred iterator: source is traversed only when the returned sequence is enumerated.
 public static IEnumerable<Transaction> FilterLargeExpenses(IEnumerable<Transaction> source,decimal threshold) {
  ArgumentNullException.ThrowIfNull(source);
  foreach(var tx in source) if(tx.Amount>=threshold) yield return tx;
 }
}