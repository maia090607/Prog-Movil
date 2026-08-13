using BLL;
using RiegoAPI.Controllers;

var builder = WebApplication.CreateBuilder(args);

// --- 0. Conexión a Firebase Realtime Database ---
// El DAL lee la URL desde App.config; desde la API se inyecta vía appsettings.json
DAL.FirebaseSettings.DatabaseUrlOverride = builder.Configuration["Firebase:DatabaseUrl"] ?? "";
DAL.FirebaseSettings.AuthTokenOverride = builder.Configuration["Firebase:AuthToken"] ?? "";
Console.WriteLine($"🔥 Conectando a Firebase: {DAL.FirebaseSettings.DatabaseUrl}");

// --- 1. CORS (Permitir conexión desde la UI) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// --- 2. Servicios Básicos ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// --- 3. Servicios de Negocio (BLL) ---
builder.Services.AddScoped<ServiciosPlanta>();
builder.Services.AddScoped<ServicioHistorial>();
builder.Services.AddScoped<ServiciosUsuario>();
builder.Services.AddScoped<ServiciosAlertas>();
builder.Services.AddScoped<ServicioClima>();
builder.Services.AddScoped<ServiciosHumedad>();
builder.Services.AddScoped<ServicioGraficas>();
builder.Services.AddScoped<ServicioTemperatura>();

// --- 4. SERVICIO SINGLETON DEL PUERTO SERIAL (CRÍTICO) ---
// Este servicio mantendrá la conexión viva con el Arduino
builder.Services.AddSingleton<ServicioPuerto>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    // Asegúrate que el COM coincida con tu administrador de dispositivos
    string puerto = config.GetValue<string>("SerialPort:Puerto") ?? "COM11";
    int baudRate = config.GetValue<int>("SerialPort:BaudRate", 9600);
    return new ServicioPuerto(puerto, baudRate);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Mensaje de inicio en consola
var puertoConfig = app.Configuration.GetValue<string>("SerialPort:Puerto") ?? "COM11";
Console.WriteLine($"✅ API Iniciada. Conectando a Arduino en {puertoConfig}...");

app.Run();