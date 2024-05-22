using FluentValidation;
using System;

namespace VaquinhaAnimal.Domain.Entities.Validations
{
    public class DoacaoValidation : AbstractValidator<Doacao>
    {
        private DateTime dataAtual = DateTime.Now;
        public DoacaoValidation()
        {
            RuleFor(c => c.Data)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.Valor)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido")
                .GreaterThan(0).WithMessage("O campo {PropertyName} precisa ser maior que {ComparisonValue}");

            RuleFor(c => c.ValorBeneficiario)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido")
                .GreaterThan(0).WithMessage("O campo {PropertyName} precisa ser maior que {ComparisonValue}");

            RuleFor(c => c.ValorPlataforma)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido")
                .GreaterThan(0).WithMessage("O campo {PropertyName} precisa ser maior que {ComparisonValue}");

            RuleFor(c => c.ValorDestinadoPlataforma)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido")
                .GreaterThanOrEqualTo(0).WithMessage("O campo {PropertyName} precisa ser maior ou igual a {ComparisonValue}");

            RuleFor(c => c.ValorTaxa)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido")
                .GreaterThan(0).WithMessage("O campo {PropertyName} precisa ser maior que {ComparisonValue}");

            RuleFor(c => c.FormaPagamento)
                .NotEmpty().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.Status)
                .NotEmpty().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.Transacao_Id)
                .NotEmpty().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.Usuario_Id)
                .NotEmpty().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.Customer_Id)
                .NotEmpty().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.Charge_Id)
                .NotEmpty().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.Campanha_Id)
                .NotEmpty().WithMessage("O campo {PropertyName} precisa ser fornecido");
        }
    }
}
