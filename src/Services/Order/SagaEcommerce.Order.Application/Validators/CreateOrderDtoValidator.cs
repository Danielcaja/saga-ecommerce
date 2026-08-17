using FluentValidation;
using SagaEcommerce.Order.Application.DTOs;

namespace SagaEcommerce.Order.Application.Validators;

public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId is required and cannot be empty.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.Total)
            .GreaterThan(0).WithMessage("The order Total must be greater than zero.");
    }
}
