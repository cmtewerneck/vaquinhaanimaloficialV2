export class Assinatura {
    id!: string;
    billing_day!: number;
    start_at!: Date;
    canceled_at!: Date;
    status!: string;
    items!: ItemsAssinatura[];
}

export class ItemsAssinatura {
    pricing_scheme!: PricingAssinaturas;
}

export class PricingAssinaturas {
    price!: number;
}