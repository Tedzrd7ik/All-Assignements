using System.Text.Json;
using BudgetTracker.Core.Models;
namespace BudgetTracker.Core.Services;
public sealed class TransactionRepository {
 public Result<IReadOnlyList<Transaction>> Load(string path) {
  if(string.IsNullOrWhiteSpace(path)) return Result<IReadOnlyList<Transaction>>.Failure("File path is required.");
  try {
   using var reader=new StreamReader(path); // this method owns and disposes the reader
   var rows=JsonSerializer.Deserialize<List<Transaction>>(reader.ReadToEnd(),new JsonSerializerOptions{PropertyNameCaseInsensitive=true});
   if(rows is null) return Result<IReadOnlyList<Transaction>>.Failure("JSON content was empty or null.");
   foreach(var tx in rows) {
    var valid=Transaction.Create(tx.Id,tx.Description,tx.Category,tx.Amount,tx.Date,tx.IsRecurring);
    if(!valid.IsSuccess) return Result<IReadOnlyList<Transaction>>.Failure($"Invalid transaction '{tx.Id}': {valid.Error}");
   }
   return Result<IReadOnlyList<Transaction>>.Success(rows.ToArray());
  } catch(FileNotFoundException) { return Result<IReadOnlyList<Transaction>>.Failure($"File not found: {path}"); }
    catch(DirectoryNotFoundException) { return Result<IReadOnlyList<Transaction>>.Failure($"Directory not found: {path}"); }
    catch(JsonException ex) { return Result<IReadOnlyList<Transaction>>.Failure($"Malformed JSON: {ex.Message}"); }
    catch(IOException ex) { return Result<IReadOnlyList<Transaction>>.Failure($"File read failed: {ex.Message}"); }
 }
}