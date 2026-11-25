export type TipoPessoa = 'F' | 'J';

export interface EnderecoConsulta {
  cep: string | null;
  logradouro: string | null;
  numero: string | null;
  complemento: string | null;
  bairro: string | null;
  cidade: string | null;
  uf: string | null;
}

export interface EmpresaConsulta {
  cnpj: string;
  razaoSocial: string;
  nomeFantasia: string | null;
  endereco: EnderecoConsulta;
}

export interface DadosCliente extends EnderecoConsulta {
  tipoPessoa: TipoPessoa;
  nome: string;
  nomeFantasia: string | null;
  cpfCnpj: string;
  telefone: string | null;
  email: string | null;
}

export interface Cliente extends DadosCliente {
  id: number;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm: string | null;
}

export interface FiltroClientes {
  busca?: string;
  tipoPessoa?: TipoPessoa;
  ativo?: boolean;
  pagina: number;
  tamanhoPagina: number;
}

export interface ClientesPaginados {
  itens: Cliente[];
  total: number;
  pagina: number;
  tamanhoPagina: number;
}
