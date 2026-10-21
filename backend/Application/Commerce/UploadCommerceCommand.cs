namespace IRoute.Application.Commerce;

public sealed record UploadCommerceCommand(Stream Content, string FileName);
