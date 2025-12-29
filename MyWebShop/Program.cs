using MyWebShop.Services;
using MyWebShop.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Ajouter les contrôleurs
builder.Services.AddControllers();

// 2. Configurer le CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => 
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 3. ENREGISTRER LE SERVICE (C'est ici qu'il doit être, en dehors du CORS)
builder.Services.AddScoped<IOdooService, OdooService>();

var app = builder.Build();   

// 4. Utiliser le CORS et mapper les routes
app.UseCors("AllowAll");
app.MapControllers();

app.Run();