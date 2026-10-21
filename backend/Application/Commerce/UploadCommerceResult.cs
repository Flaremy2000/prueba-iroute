namespace IRoute.Application.Commerce;

public sealed record UploadCommerceResult(
    string FileName,
    DateOnly ProcessDate,
    int RowsRead,
    int RowsInserted);
