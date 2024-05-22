using System.Collections.Generic;

namespace VaquinhaAnimal.Domain.Entities.Pagarme
{
    public class PagarmePedidoGeral
    {
        public class PagarmePedido
        {
            public List<Itens> items { get; set; }
            public string customer_id { get; set; }
			public Customer customer { get; set; }
            public List<Pagamentos> payments { get; set; }
        }
		
		public class Customer
        {
            public string name { get; set; }
			public string email { get; set; }
			public string type { get; set; }
			public string document { get; set; }
        }
		
		public class Phones
        {
            public Home_Phone home_phone { get; set; }
        }
		
		public class Home_Phone
        {
            public string country_code { get; set; }
			public string area_code { get; set; }
			public string number { get; set; }
        }

        public class Itens
        {
            public int amount { get; set; }
            public string description { get; set; }
            public int quantity { get; set; }
            public string code { get; set; }
        }

        public class Pagamentos
        {
            public string payment_method { get; set; }
            public CartaoCredito? credit_card { get; set; }
            public Pix? pix { get; set; }
			//public Boleto? boleto { get; set; }
        }

        public class Pix
        {
            public int expires_in { get; set; }
            public List<PixAdditional> additional_information { get; set; }
        }

        public class PixAdditional
        {
            public string name { get; set; }
            public string value { get; set; }
        }

        public class CartaoCredito
        {
            public bool recurrence { get; set; }
            public int installments { get; set; }
            public string statement_descriptor { get; set; }
            public string card_id { get; set; }
            public CartaoCreditoUsado card { get; set; }
        }

        public class CartaoCreditoUsado
        {
            public string cvv { get; set; }
			public int exp_year { get; set; }
			public int exp_month { get; set; }
			public string holder_name { get; set; }
			public string number { get; set; }
        }
    }

}