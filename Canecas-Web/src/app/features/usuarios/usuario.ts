export interface Usuario {
  id: number;
  nome: string;
  ativo: boolean;
  criadoEm: string;
}

export interface CadastrarUsuario {
  nome: string;
  senha: string;
}

export interface EditarUsuario {
  nome: string;
  senha?: string;
}
