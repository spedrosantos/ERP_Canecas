const USUARIO = "Jana";
const WHATSAPP = "5511979587198";

const MENU = [
  { icone: "layout-dashboard", nome: "Dashboard", link: "index.html", pag: "dashboard" },
  { icone: "shopping-cart", nome: "Pedidos", sub: [
    { nome: "Todos os pedidos", link: "#" },
    { nome: "Novo pedido", link: "#" } ] },
  { icone: "folder", nome: "Cadastros", sub: [
    { nome: "Clientes", link: "#" },
    { nome: "Produtos", link: "#" },
    { nome: "Categorias", link: "#" } ] },
  { icone: "package", nome: "Estoque", sub: [
    { nome: "Produtos em estoque", link: "#" },
    { nome: "Entradas e saídas", link: "#" } ] },
  { icone: "coin", nome: "Financeiro", sub: [
    { nome: "Contas a receber", link: "#" },
    { nome: "Contas a pagar", link: "#" } ] },
  { icone: "chart-bar", nome: "Relatórios", link: "#" },
  { icone: "settings", nome: "Configurações", sub: [
    { nome: "Visão geral", link: "configuracoes.html", pag: "configuracoes" },
    { nome: "Usuários", link: "#" },
    { nome: "Permissões", link: "#" } ] }
];

const pagina = document.body.dataset.pagina || "";

const lista = MENU.map(m => {
  const busca = (m.nome + " " + (m.sub || []).map(s => s.nome).join(" ")).toLowerCase();
  if (!m.sub) {
    const ativo = m.pag === pagina ? " ativo" : "";
    return `<a class="item${ativo}" href="${m.link}" data-busca="${busca}"><i class="ti ti-${m.icone}"></i><span class="txt oculta">${m.nome}</span></a>`;
  }
  const temAtivo = m.sub.some(s => s.pag === pagina);
  const subs = m.sub.map(s => `<a class="${s.pag === pagina ? "ativo" : ""}" href="${s.link}">${s.nome}</a>`).join("");
  return `<div class="grupo${temAtivo ? " aberto" : ""}" data-busca="${busca}" data-ativo="${temAtivo ? 1 : 0}">
    <button class="item grupo-titulo" type="button"><i class="ti ti-${m.icone}"></i><span class="txt oculta">${m.nome}</span><i class="ti ti-chevron-down seta oculta"></i></button>
    <div class="sub">${subs}</div>
  </div>`;
}).join("");

document.body.insertAdjacentHTML("afterbegin", `
<header class="topbar">
  <a class="marca" href="index.html"><img src="img/caneca.png" alt=""><span><b>Bora</b>cup</span></a>
  <div class="busca"><i class="ti ti-search"></i><input type="text" id="buscaMenu" placeholder="Pesquisar no menu"></div>
</header>
<aside class="menu">
  <div class="menu-topo"><button class="pin oculta" id="btnPin" type="button" title="Fixar menu"><i class="ti ti-pin"></i></button></div>
  <nav class="menu-lista">${lista}</nav>
  <div class="menu-rodape">
    <a class="item zap" href="https://wa.me/${WHATSAPP}" target="_blank" rel="noopener"><i class="ti ti-brand-whatsapp"></i><span class="txt oculta">WhatsApp</span></a>
    <div class="usuario"><div class="avatar">${USUARIO[0]}</div><div class="txt oculta"><strong>${USUARIO}</strong><small>Usuário logado</small></div></div>
    <a class="item sair" href="login.html"><i class="ti ti-logout"></i><span class="txt oculta">Sair</span></a>
  </div>
</aside>`);

const menu = document.querySelector(".menu");
const busca = document.getElementById("buscaMenu");
const pin = document.getElementById("btnPin");

menu.querySelectorAll(".grupo-titulo").forEach(b => {
  b.addEventListener("click", () => b.parentElement.classList.toggle("aberto"));
});

function fixar(sim) {
  menu.classList.toggle("fixo", sim);
  document.body.classList.toggle("menu-fixo", sim);
  pin.innerHTML = `<i class="ti ti-${sim ? "pinned" : "pin"}"></i>`;
  try { localStorage.setItem("menuFixo", sim ? "1" : "0"); } catch (e) {}
}
pin.addEventListener("click", () => fixar(!menu.classList.contains("fixo")));

let salvo = false;
try { salvo = localStorage.getItem("menuFixo") === "1"; } catch (e) {}
fixar(salvo);

busca.addEventListener("input", () => {
  const t = busca.value.toLowerCase().trim();
  menu.classList.toggle("aberto", t !== "");
  menu.querySelectorAll("[data-busca]").forEach(el => {
    el.style.display = el.dataset.busca.includes(t) ? "" : "none";
    if (el.classList.contains("grupo")) {
      el.classList.toggle("aberto", t !== "" || el.dataset.ativo === "1");
    }
  });
});

function marcarGrupo() {
  menu.querySelectorAll(".grupo").forEach(g => {
    g.classList.toggle("tem-ativo", !!g.querySelector(".sub a.ativo"));
  });
}
marcarGrupo();

menu.querySelectorAll("a.item:not(.zap):not(.sair), .sub a").forEach(a => {
  a.addEventListener("click", () => {
    menu.querySelectorAll(".item.ativo, .sub a.ativo").forEach(x => x.classList.remove("ativo"));
    a.classList.add("ativo");
    marcarGrupo();
  });
});