export class Order {
    items!: Items[];
    payments!: Payments[];
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
    cvv!: string;
}