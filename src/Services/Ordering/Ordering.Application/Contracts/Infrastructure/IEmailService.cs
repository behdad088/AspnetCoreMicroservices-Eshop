using Ordering.Application.Models;

namespace Ordering.Application.Contracts.Infrastructure
{
    internal interface IEmailService
    {
        Task<bool> SendEmailAsync(Email email);
    }
}
