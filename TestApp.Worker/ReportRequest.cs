namespace TestApp.Worker;

/// <summary>
/// The message contract between the API and this worker.
///
/// Deliberately duplicated rather than shared from a common assembly: producer and
/// consumer deploy independently, so the contract is a versioned schema they agree
/// on, not a type they share. In a larger system this would live in a published
/// contracts package.
/// </summary>
/// <param name="JobId">Correlates the queued work with the caller's job id.</param>
/// <param name="Subject">What the report is about. The value "fail" deliberately
/// throws, so the retry and dead-letter path can be demonstrated.</param>
/// <param name="WorkSeconds">How long to pretend the work takes.</param>
public sealed record ReportRequest(string JobId, string Subject, int WorkSeconds);
