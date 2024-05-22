export class OrderAssinatura {
    card_id!: string;
    customer!: Customer;
    card!: Card;
    items!: Items[];
}

export class Customer {
    name!: string;    
    email!: string;
    type!: string;
    document!: string;
}

export class Items {
    description!: string;
    quantity!: number;
    pricing_scheme!: RecorrenciaScheme;
}

export class RecorrenciaScheme {
    scheme_type!: string;
    price!: number;
}

export class Card {
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