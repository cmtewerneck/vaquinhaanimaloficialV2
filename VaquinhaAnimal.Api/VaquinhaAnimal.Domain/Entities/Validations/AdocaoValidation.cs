using FluentValidation;

namespace VaquinhaAnimal.Domain.Entities.Validations
{
    public class AdocaoValidation : AbstractValidator<Adocao>
    {
        public AdocaoValidation()
        {
            RuleFor(c => c.NomePet)
                .NotEmpty().WithMessage("O campo {PropertyName} precisa ser fornecido")
                .Length(3, 50).WithMessage("O campo {PropertyName} precisa ter entre {MinLength} e {MaxLength} caracteres");

            RuleFor(c => c.FaixaEtaria)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.Celular)
                .Length(6, 20).When(c => c.Celular != null).WithMessage("O campo {PropertyName} precisa ter entre {MinLength} e {MaxLength} caracteres");

            RuleFor(c => c.Email)
                .Length(3, 100).When(c => c.Email != null).WithMessage("O campo {PropertyName} precisa ter entre {MinLength} e {MaxLength} caracteres");

            RuleFor(c => c.Descricao)
                .Length(5, 1000).When(c => c.Descricao != null).WithMessage("O campo {PropertyName} precisa ter entre {MinLength} e {MaxLength} caracteres");

            RuleFor(c => c.Abrigo_Nome)
                .Length(3, 50).When(c => c.Abrigo_Nome != null && c.Abrigo_Nome != "").WithMessage("O campo {PropertyName} precisa ter entre {MinLength} e {MaxLength} caracteres");

            RuleFor(c => c.Empresa_Nome)
               .Length(3, 50).When(c => c.Empresa_Nome != null && c.Empresa_Nome != "").WithMessage("O campo {PropertyName} precisa ter entre {MinLength} e {MaxLength} caracteres");

            RuleFor(c => c.Particular_Nome)
               .Length(3, 50).When(c => c.Particular_Nome != null && c.Particular_Nome != "").WithMessage("O campo {PropertyName} precisa ter entre {MinLength} e {MaxLength} caracteres");

            RuleFor(c => c.TipoAnunciante)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.UsuarioId)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.Castrado)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.TipoPet)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.Adotado)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido");
        }
    }
}
