export class OrderCartaoNovo {
    recaptcha!: string;
    items!: Items[];
    payments!: Payments[];
    salvarCartao: boolean = false;
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
    card!: card;
}

export class card {
    number!: string;
    holder_name!: string;
    holder_document!: string;
    brand!: string;
    exp_month!: number;
    exp_year!: number;
    cvv!: string;
    billing_address!: BillingAddress;
}

export class BillingAddress {
    line_1!: string;
    zip_code!: string;
    city!: string;
    state!: string;
    country!: string;
}