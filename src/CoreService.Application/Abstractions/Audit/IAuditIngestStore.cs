namespace CoreService.Application.Abstractions.Audit;

public interface IAuditIngestStore
{
    /// <summary>
    /// Сохранить события. Дубликаты по event_id должны игнорироваться.
    /// Возвращает количество реально вставленных записей.
    /// </summary>
    Task<int> InsertIgnoreDuplicatesAsync(
        IReadOnlyList<AuditWriteEntry> events,
        CancellationToken ct = default);
}