export class Campanha {
    id!: string;
    data_inicio?: Date;
    data_encerramento?: Date;
    duracao_dias?: number;
    tag_campanha!: TagCampanhaEnum;
    tipo_campanha!: TipoCampanhaEnum;
    titulo!: string;    
    descricao_curta!: string;
    descricao_longa!: string;
    url_campanha!: string;
    valor_desejado!: number;
    total_arrecadado?: number;
    termos!: boolean;
    premium!: boolean;
    status_campanha?: number;
    percentual_arrecadado?: number;
    restam?: number;
    totalDoadores?: number;
    usuario_id!: string;
    video_url!: string;
    
    beneficiario!: Beneficiario;
    imagens!: Imagem[];
}

export class Beneficiario {
    id!: string;
    nome!: string;
    documento!: string;
    tipo!: string;
    codigo_banco!: string;
    numero_agencia!: string;
    digito_agencia!: string;
    numero_conta!: string;
    digito_conta!: string;
    tipo_conta!: string;
    recebedor_id!: string;
    campanha_id!: string;
}

export class Imagem {
    id!: string;
    tipo!: TipoImagemEnum;
    arquivo!: string;
    arquivo_upload!: string;
    campanha_id!: string;
}

export enum TipoImagemEnum {
    Capa = 1,
    Interna = 2
}

export enum TagCampanhaEnum {
    Alimentacao = 1,
    Medicamentos = 2,
    Cirurgias = 3,
    Tratamentos = 4,
    Acessórios = 5,
    PuroAmor = 6,
    Outros = 7
}

export enum TipoCampanhaEnum {
    Recorrente = 1,
    Unica = 2
}