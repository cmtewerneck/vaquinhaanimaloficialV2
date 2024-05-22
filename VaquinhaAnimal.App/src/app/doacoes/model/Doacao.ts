export class Doacao {
    id!: string;
    data!: Date;
    valor!: number;
    forma_pagamento!: string;
    status!: string;
    transacao_id!: string;
    url_download!: string;
    customer_id!: string;
    usuario_id!: string;
    campanha_id!: string;
    campanha!: Campanha;
}

export class Campanha {
    titulo!: string;
}