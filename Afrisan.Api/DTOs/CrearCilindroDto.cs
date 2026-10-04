using System.ComponentModel.DataAnnotations;

namespace Afrisan.Api.DTOs
{
    public class CrearCilindroDto : IValidatableObject
    {
        [Required(ErrorMessage = "Debes seleccionar un gas refrigerante.")]
        public int? GasRefrigeranteId { get; set; }

        [Required(ErrorMessage = "Debes ingresar la capacidad del cilindro.")]
        public decimal? CapacidadKg { get; set; }

        [Required(ErrorMessage = "Debes ingresar la tara del cilindro.")]
        public decimal? TaraKg { get; set; }

        [Required(ErrorMessage = "Debes ingresar el peso bruto actual.")]
        public decimal? PesoActualKg { get; set; }


        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            // CAPACIDAD
            if (CapacidadKg.HasValue &&
                (CapacidadKg.Value <= 0 ||
                 CapacidadKg.Value > 1000000))
            {
                yield return new ValidationResult(
                    "La capacidad debe ser mayor que cero y no superar 1.000.000 kg.",
                    new[] { nameof(CapacidadKg) }
                );
            }


            // TARA
            if (TaraKg.HasValue &&
                (TaraKg.Value <= 0 ||
                 TaraKg.Value > 1000000))
            {
                yield return new ValidationResult(
                    "La tara debe ser mayor que cero y no superar 1.000.000 kg.",
                    new[] { nameof(TaraKg) }
                );
            }


            // PESO BRUTO ACTUAL
            if (PesoActualKg.HasValue &&
                (PesoActualKg.Value < 0 ||
                 PesoActualKg.Value > 1000000))
            {
                yield return new ValidationResult(
                    "El peso bruto actual debe estar entre 0 y 1.000.000 kg.",
                    new[] { nameof(PesoActualKg) }
                );
            }


            // EL PESO BRUTO NO PUEDE SER MENOR QUE LA TARA
            if (TaraKg.HasValue &&
                PesoActualKg.HasValue &&
                PesoActualKg.Value < TaraKg.Value)
            {
                yield return new ValidationResult(
                    "El peso bruto actual no puede ser menor que la tara del cilindro.",
                    new[] { nameof(PesoActualKg) }
                );
            }


            // EL PESO BRUTO NO PUEDE SUPERAR:
            // TARA + CAPACIDAD DE REFRIGERANTE
            if (TaraKg.HasValue &&
                CapacidadKg.HasValue &&
                PesoActualKg.HasValue)
            {
                var pesoBrutoMaximo =
                    TaraKg.Value + CapacidadKg.Value;

                if (PesoActualKg.Value > pesoBrutoMaximo)
                {
                    yield return new ValidationResult(
                        $"El peso bruto actual no puede superar " +
                        $"la tara más la capacidad del cilindro " +
                        $"({pesoBrutoMaximo:0.##} kg).",
                        new[] { nameof(PesoActualKg) }
                    );
                }
            }
        }
    }
}