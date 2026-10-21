using System.ComponentModel.DataAnnotations;

namespace IRoute.Api.Contracts;

public sealed class ProcessCommerceRequest
{
    [Required]
    public DateOnly ProcessDate { get; init; }
}
