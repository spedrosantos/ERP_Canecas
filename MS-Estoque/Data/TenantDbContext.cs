using Microsoft.EntityFrameworkCore;

namespace MS_Estoque.Data;

public class TenantDbContext(
    DbContextOptions<TenantDbContext> options)
    : DbContext(options)
{
}