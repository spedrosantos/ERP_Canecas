namespace MS_Compra.Services;

public sealed class PedidoSituacaoException()
    : Exception("Somente pedidos pendentes podem ser modificados.");
