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

export interface DadosFornecedor extends EnderecoConsulta {
  tipoPessoa: TipoPessoa;
  nome: string;
  nomeFantasia: string | null;
  cpfCnpj: string;
  telefone: string | null;
  email: string | null;
}

export interface Fornecedor extends DadosFornecedor {
  id: number;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm: string | null;
}

export interface FiltroFornecedores {
  busca?: string;
  tipoPessoa?: TipoPessoa;
  ativo?: boolean;
  pagina: number;
  tamanhoPagina: number;
}

export interface FornecedoresPaginados {
  itens: Fornecedor[];
  total: number;
  pagina: number;
  tamanhoPagina: number;
}
