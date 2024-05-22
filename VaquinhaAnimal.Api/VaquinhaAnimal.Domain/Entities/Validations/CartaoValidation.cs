using FluentValidation;

namespace VaquinhaAnimal.Domain.Entities.Validations
{
    public class CartaoValidation : AbstractValidator<Cartao>
    {
        public CartaoValidation()
        {
            RuleFor(c => c.Card_Id)
                .NotEmpty().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.Customer_Id)
                .NotEmpty().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.Exp_Month)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.Exp_Year)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.First_Six_Digits)
                .NotEmpty().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.Last_Four_Digits)
                .NotEmpty().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.Status)
                .NotEmpty().WithMessage("O campo {PropertyName} precisa ser fornecido");
        }
    }
}
