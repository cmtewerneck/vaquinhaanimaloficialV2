using System.Collections.Generic;

namespace VaquinhaAnimal.Domain.Entities.Pagarme
{
    public class PagarmeResponse<T>
    {
        public List<T> data { get; set; }
        public Paging paging { get; set; }
    }

    public class Paging
    {
        public int total { get; set; }
    }
}