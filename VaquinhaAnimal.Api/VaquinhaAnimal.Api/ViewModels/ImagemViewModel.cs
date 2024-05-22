using System;

namespace VaquinhaAnimal.Api.ViewModels
{
    public class ImagemViewModel
    {
        public Guid id { get; set; }
        public int tipo { get; set; } 
        public string arquivo { get; set; } 
        public string arquivo_upload { get; set; } 
        public Guid campanha_id { get; set; } 
    }
}
