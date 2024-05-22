export class OrderCartao {
    customer!: Customer;
    items!: Items[];
    payments!: Payments[];
    valorPlataforma!: number;
}

export class Customer {
    name!: string;    
    email!: string;
    type!: string;
    document!: string;
}

export class Items {
    amount!: number;
    description!: string;
    quantity!: number;
    code!: string;
}

export class Payments {
    payment_method!: string;
    credit_card!: CreditCard;
}

export class CreditCard {
    recurrence!: boolean;
    installments!: number;
    statement_descriptor!: string;
    card_id!: string;
    card!: card;
}

export class card {
    number!: string;
    exp_month!: number;
    exp_year!: number;
    cvv!: string;
    holder_name!: string;
    holder_document!: string;
    billing_address!: BillingAddress;
}

export class BillingAddress {
    line_1!: string;
    zip_code!: string;
    city!: string;
    state!: string;
    country!: string;
}