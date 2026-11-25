const PERGUNTAS = [
  { p: "Como criar um novo pedido?", r: "Clique em Novo pedido no Dashboard, escolha o cliente, adicione os produtos e confirme." },
  { p: "Como cadastrar um cliente?", r: "Vá em Cadastros > Clientes, clique em Novo cliente, preencha os dados e salve." },
  { p: "Como cadastrar um produto?", r: "Vá em Cadastros > Produtos, clique em Novo produto e informe nome, preço e quantidade em estoque." },
  { p: "Como consultar o estoque?", r: "Abra Estoque > Produtos em estoque. Os itens com estoque baixo também aparecem no Dashboard." },
  { p: "Como adicionar um usuário?", r: "Vá em Configurações > Usuários e clique em Novo usuário. As permissões ficam em Configurações > Permissões." },
  { p: "Esqueci minha senha", r: "Procure o administrador do sistema ou fale com o suporte pelo WhatsApp." }
];

document.body.insertAdjacentHTML("beforeend", `
<button class="ajuda-btn" id="ajudaBtn" type="button" aria-label="Ajuda"><i class="ti ti-help"></i></button>
<div class="ajuda-painel" id="ajudaPainel">
  <div class="ajuda-topo">
    <div><strong>Em que posso te ajudar?</strong><small>Escolha uma pergunta abaixo</small></div>
    <button type="button" id="ajudaFechar" aria-label="Fechar"><i class="ti ti-x"></i></button>
  </div>
  <div class="ajuda-corpo" id="ajudaCorpo"></div>
</div>`);

const ajudaBtn = document.getElementById("ajudaBtn");
const ajudaPainel = document.getElementById("ajudaPainel");
const ajudaCorpo = document.getElementById("ajudaCorpo");

function mostrarLista() {
  ajudaCorpo.innerHTML = PERGUNTAS.map((q, i) =>
    `<button type="button" class="ajuda-pergunta" data-i="${i}">${q.p}</button>`).join("") +
    `<a class="ajuda-zap" href="https://wa.me/${WHATSAPP}" target="_blank" rel="noopener"><i class="ti ti-brand-whatsapp"></i> Falar com o suporte</a>`;
}

function mostrarResposta(i) {
  ajudaCorpo.innerHTML = `<p class="ajuda-pergunta-txt">${PERGUNTAS[i].p}</p><p>${PERGUNTAS[i].r}</p>
    <button type="button" class="ajuda-voltar"><i class="ti ti-arrow-left"></i> Voltar às perguntas</button>`;
}

ajudaCorpo.addEventListener("click", e => {
  const q = e.target.closest(".ajuda-pergunta");
  if (q) mostrarResposta(q.dataset.i);
  if (e.target.closest(".ajuda-voltar")) mostrarLista();
});

ajudaBtn.addEventListener("click", () => ajudaPainel.classList.toggle("aberto"));
document.getElementById("ajudaFechar").addEventListener("click", () => ajudaPainel.classList.remove("aberto"));
mostrarLista();