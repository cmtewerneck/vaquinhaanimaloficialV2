export class OrderPix {
    items!: Items[];
    payments!: Payments[];
    valorPlataforma!: number;
}

export class Items {
    amount!: number;    
    description!: string;
    quantity!: number;
    code!: string;
}

export class Payments {
    payment_method!: string;
    pix!: Pix;
}

export class Pix {
    expires_in!: number;
    additional_information!: AddInformation[];
}

export class AddInformation {
    name!: string;
    value!: string;
}