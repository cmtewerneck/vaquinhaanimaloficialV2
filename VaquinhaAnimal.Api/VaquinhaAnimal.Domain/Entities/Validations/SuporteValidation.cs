using FluentValidation;

namespace VaquinhaAnimal.Domain.Entities.Validations
{
    public class SuporteValidation : AbstractValidator<Suporte>
    {
        public SuporteValidation()
        {
            RuleFor(c => c.Data)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.Usuario_Id)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.Assunto)
                .NotEmpty().WithMessage("O campo {PropertyName} precisa ser fornecido")
                .Length(3, 100).WithMessage("O campo {PropertyName} precisa ter entre {MinLength} e {MaxLength} caracteres");

            RuleFor(c => c.Mensagem)
                .NotEmpty().WithMessage("O campo {PropertyName} precisa ser fornecido")
                .Length(3, 500).WithMessage("O campo {PropertyName} precisa ter entre {MinLength} e {MaxLength} caracteres");
            
            RuleFor(c => c.Resposta)
                .Length(3, 1000).When(c => c.Resposta != "").WithMessage("O campo {PropertyName} precisa ter entre {MinLength} e {MaxLength} caracteres");

            RuleFor(c => c.Respondido)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido");
        }
    }
}
