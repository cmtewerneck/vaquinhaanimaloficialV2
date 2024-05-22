namespace VaquinhaAnimal.Domain.Entities.Pagarme
{
    public class PagarmeRecebedor
    {
        public string name { get; set; }
        public string document { get; set; }
        public string type { get; set; }
        public PagarmeRecebedorBankAcoount default_bank_account { get; set; }
        public PagarmeRecebedorTransferSettings transfer_settings { get; set; }
        public PagarmeRecebedorAntecipationSettings automatic_anticipation_settings { get; set; }
    }

    public class PagarmeRecebedorBankAcoount
    {
        public string holder_name { get; set; }
        public string bank { get; set; }
        public string branch_number { get; set; }
        public string branch_check_digit { get; set; }
        public string account_number { get; set; }
        public string account_check_digit { get; set; }
        public string holder_type { get; set; }
        public string holder_document { get; set; }
        public string type { get; set; }
    }

    public class PagarmeRecebedorTransferSettings
    {
        public bool transfer_enabled { get; set; }
        public string transfer_interval { get; set; }
        public int transfer_day { get; set; }
    }

    public class PagarmeRecebedorAntecipationSettings
    {
        public bool enables { get; set; }
    }
}