using Microsoft.EntityFrameworkCore;
using GetAdultById.Data;
using GetAdultById.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<DataContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

var app = builder.Build();

app.UseHttpsRedirection();

async Task<Adult?> GetAdultById(DataContext context, int id) => await context.Adults.FindAsync(id);
app.MapGet("/Adult/{id}", async (DataContext context, int id) => await GetAdultById(context, id))
.WithName("GetAdultById")
.WithOpenApi();

app.Run();