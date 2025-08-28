using FluentValidation;
using RealEstate.Contracts.Dtos;

namespace RealEstate.Api.Validation
{
    public sealed class PropertyFilterValidator : AbstractValidator<PropertyFilterDto>
    {
        public PropertyFilterValidator()
        {
            // Estos límites son coherentes con tu Service (page>=1; pageSize clamp 1..100)
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("Page must be >= 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");

            // Longitudes amplias para no romper comportamiento; ajustaremos si hace falta.
            RuleFor(x => x.Name)
                .Must(s => s is null || s.Length <= 100).WithMessage("Name is too long (max 100).");

            RuleFor(x => x.Address)
                .Must(s => s is null || s.Length <= 120).WithMessage("Address is too long (max 120).");

            // Misma regla que ya impone tu Service (MinPrice <= MaxPrice).
            RuleFor(x => x)
                .Must(x => x.MinPrice is null || x.MaxPrice is null || x.MinPrice <= x.MaxPrice)
                .WithMessage("MinPrice cannot be greater than MaxPrice.");
        }
    }
}
