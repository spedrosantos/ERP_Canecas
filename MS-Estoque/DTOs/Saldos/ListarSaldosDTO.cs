using System.ComponentModel.DataAnnotations;

namespace MS_Estoque.DTOs.Saldos;

public class ListarSaldosDTO
{
    [MaxLength(150)]
    public string? Busca { get; set; }

    public bool? AbaixoDoMinimo { get; set; }

    [Range(1, int.MaxValue)]
    public int Pagina { get; set; } = 1;

    [Range(1, 100)]
    public int TamanhoPagina { get; set; } = 20;
}
