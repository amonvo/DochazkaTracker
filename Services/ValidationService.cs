namespace DochazkaTracker.Services
{
    public class ValidationService
    {
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
            public List<string> Warnings { get; set; } = new List<string>();
        }

        public ValidationResult ValidateDochazka(Dochazka dochazka)
        {
            var result = new ValidationResult { IsValid = true };

            if (!dochazka.Odchod.HasValue)
            {
                result.Errors.Add("Odchod musí být vyplněn");
                result.IsValid = false;
            }

            if (dochazka.Odchod <= dochazka.Prichod)
            {
                result.Errors.Add("Odchod musí být později než příchod");
                result.IsValid = false;
            }

            if (dochazka.Rozdil.TotalHours > 16)
            {
                result.Errors.Add("Pracovní doba přesahuje 16 hodin");
                result.IsValid = false;
            }

            return result;
        }
    }
}
