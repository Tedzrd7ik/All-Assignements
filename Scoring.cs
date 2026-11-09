using BudgetTracker.Core.Models;
namespace BudgetTracker.Core.Services;
public sealed record ScoreConfiguration(decimal AmountWeight,decimal RecurringBonus);
public static class Scoring {
 // Currying: first configuration, then a transaction -> score.
 public static Func<Transaction,decimal> CreateScore(ScoreConfiguration config) {
  ArgumentNullException.ThrowIfNull(config);
  return tx=>decimal.Round(tx.Amount*config.AmountWeight+(tx.IsRecurring?config.RecurringBonus:0m),2);
 }
 public static Func<Transaction,decimal> CreateLowRiskScore=>CreateScore(new(1.0m,5m));
 public static Func<Transaction,decimal> CreateHighValueScore=>CreateScore(new(1.5m,0m));
}