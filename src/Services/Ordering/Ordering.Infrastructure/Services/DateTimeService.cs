using Ordering.Application.Contracts.Services;

namespace Ordering.Infrastructure.Services
{
    public class DateTimeService : IDateTime
    {
        public DateTime Now => DateTime.Now;
    }
}
