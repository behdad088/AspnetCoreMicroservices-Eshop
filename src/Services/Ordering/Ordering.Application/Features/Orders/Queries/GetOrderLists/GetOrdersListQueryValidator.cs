using FluentValidation;

namespace Ordering.Application.Features.Orders.Queries.GetOrderLists
{
    public class GetOrdersListQueryValidator : AbstractValidator<GetOrdersListQuery>
    {
        public GetOrdersListQueryValidator()
        {
            RuleFor(p => p.Username)
                .NotEmpty().WithMessage("{Username} is required.");
        }
    }
}
