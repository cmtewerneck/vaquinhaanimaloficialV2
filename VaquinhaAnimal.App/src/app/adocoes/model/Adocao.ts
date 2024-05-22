export class Adocao {
    id!: string;
    nome_pet!: string;
    email!: string;
    celular!: string;
    instagram!: string;
    facebook!: string;
    tipo_pet!: TipoPetEnum;
    tipo_anunciante!: TipoAnuncianteEnum;
    faixa_etaria!: FaixaEtariaEnum;
    castrado!: boolean;
    abrigo_nome!: string;
    particular_nome!: string;
    empresa_nome!: string;
    adotado!: boolean;
    foto!: string;
    foto_upload!: string;
    link_video!: string;
    descricao!: string;
    usuario_id!: string;
    url_adocao!: string;
}

export enum TipoPetEnum {
    Cachorro = 1,
    Gato = 2,
    Passaro = 3,
    Coelho = 4,
    Outros = 5
}

export enum FaixaEtariaEnum {
    Faixa01 = 1,
    Faixa02 = 2,
    Faixa03 = 3,
    Faixa04 = 4,
    Faixa05 = 5
}

export enum TipoAnuncianteEnum {
    Abrigo = 1,
    Empresa = 2,
    Particular = 3
}