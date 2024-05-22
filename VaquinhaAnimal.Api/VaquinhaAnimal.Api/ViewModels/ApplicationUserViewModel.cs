using System;

namespace VaquinhaAnimal.Api.ViewModels
{
    public class ApplicationUserViewModel
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Code { get; set; }
        public string Document { get; set; }
        public string Type { get; set; }
        public string Document_Type { get; set; }
        public string Gender { get; set; }
        public DateTime Birthdate { get; set; }
        public string Line_1 { get; set; }
        public string Line_2 { get; set; }
        public string Zip_Code { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string Home_phone_country_code { get; set; }
        public string Home_phone_area_code { get; set; }
        public string home_phone_number { get; set; }
        public string Mobile_phone_country_code { get; set; }
        public string Mobile_phone_area_code { get; set; }
        public string Mobile_phone_number { get; set; }
        public string Foto { get; set; }
        public string FotoUpload { get; set; }
    }
}
