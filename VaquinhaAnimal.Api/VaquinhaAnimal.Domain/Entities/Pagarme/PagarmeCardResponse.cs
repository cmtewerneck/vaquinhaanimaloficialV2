using System;

namespace VaquinhaAnimal.Domain.Entities.Pagarme
{
    public class PagarmeCardResponse
    {
        public string id { get; set; }
        public string last_four_digits { get; set; }
        public string first_six_digits { get; set; }
        public string brand { get; set; }
        public string holder_name { get; set; }
        public string holder_document { get; set; }
        public int exp_month { get; set; }
        public int exp_year { get; set; }
        public string status { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
    }
}