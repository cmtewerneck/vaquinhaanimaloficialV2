export class OrderBoleto {
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
    boleto!: Boleto;
}

export class Boleto {
    instructions!: string;
    due_at?: Date;
}