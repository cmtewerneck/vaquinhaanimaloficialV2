using FluentValidation;

namespace VaquinhaAnimal.Domain.Entities.Validations
{
    public class ImagemValidation : AbstractValidator<Imagem>
    {
        public ImagemValidation()
        {
            RuleFor(c => c.Tipo)
                .NotNull().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.Arquivo)
               .NotEmpty().WithMessage("O campo {PropertyName} precisa ser fornecido");

            RuleFor(c => c.Campanha_Id)
                .NotEmpty().WithMessage("O campo {PropertyName} precisa ser fornecido");
        }
    }
}
