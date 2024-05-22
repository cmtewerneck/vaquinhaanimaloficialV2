using FluentValidation;
using System;

namespace VaquinhaAnimal.Domain.Entities.Validations
{
    public class CampanhaValidation : AbstractValidator<Campanha>
    {
        private DateTime dataAtual = DateTime.Now;

        public CampanhaValidation()
        {
            RuleFor(c => c.DataCriacao)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.DuracaoDias)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido")
                .GreaterThanOrEqualTo(30).When(c => c.DuracaoDias != null).WithMessage("Sua campanha deve durar no mínimo de 30 dias")
                .LessThanOrEqualTo(120).When(c => c.DuracaoDias != null).WithMessage("Sua campanha deve durar no máximo de 120 dias");

            RuleFor(c => c.Titulo)
                .NotEmpty().WithMessage("O campo {PropertyName} precisa ser fornecido")
                .Length(3, 100).WithMessage("O campo {PropertyName} precisa ter entre {MinLength} e {MaxLength} caracteres");

            RuleFor(c => c.DescricaoCurta)
                .NotEmpty().WithMessage("O campo {PropertyName} precisa ser fornecido")
                .Length(5, 200).WithMessage("O campo {PropertyName} precisa ter entre {MinLength} e {MaxLength} caracteres");

            RuleFor(c => c.DescricaoLonga)
                .NotEmpty().WithMessage("O campo {PropertyName} precisa ser fornecido")
                .Length(500, 5000).WithMessage("O campo {PropertyName} precisa ter entre {MinLength} e {MaxLength} caracteres");

            RuleFor(c => c.ValorDesejado)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido")
                .GreaterThan(0).WithMessage("O campo {PropertyName} precisa ser maior que {ComparisonValue}");

            RuleFor(c => c.TotalArrecadado)
               .GreaterThanOrEqualTo(0).WithMessage("O campo {PropertyName} precisa ser preenchido e maior ou igual a {ComparisonValue}");

            RuleFor(c => c.Termos)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.Premium)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.StatusCampanha)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.Usuario_Id)
                .NotEmpty().WithMessage("O campo {PropertyName} precisa ser fornecido");
        }
    }
}
