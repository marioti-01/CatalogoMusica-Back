using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CatalogoMusica.WebAPI.Context;
using CatalogoMusica.WebAPI.Interfaces;
using CatalogoMusica.WebAPI.Repositories;
using CatalogoMusica.WebAPI.Services;
using CatalogoMusica.WebAPI.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ---------- Banco de dados ----------
builder.Services.AddDbContext<CatalogoMusicaContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Configure a ConnectionStrings:DefaultConnection.")));

// ---------- Repositórios ----------
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IArtistaRepository, ArtistaRepository>();
builder.Services.AddScoped<IGeneroRepository, GeneroRepository>();
builder.Services.AddScoped<IMusicaRepository, MusicaRepository>();
builder.Services.AddScoped<IPlaylistRepository, PlaylistRepository>();
builder.Services.AddScoped<IFilaRepository, FilaRepository>();

// ---------- Serviços ----------
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection(CloudinarySettings.SectionName));
builder.Services.AddScoped<IImageService, CloudinaryService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ITokenRevogadoService, TokenRevogadoService>();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// ---------- Autenticação JWT ----------
var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Configure a seção Jwt.");

if (string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience)
    || Encoding.UTF8.GetByteCount(jwt.Key) < 32 || jwt.ExpirationMinutes is < 1 or > 1440)
    throw new InvalidOperationException("Configure Jwt:Issuer, Jwt:Audience, Jwt:ExpirationMinutes (1 a 1440) e Jwt:Key (mínimo de 32 bytes).");

builder.Services.AddSingleton(jwt);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                // Logout (RF02): token encerrado não é mais aceito.
                var jti = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                var tokensRevogados = context.HttpContext.RequestServices.GetRequiredService<ITokenRevogadoService>();
                if (string.IsNullOrEmpty(jti) || tokensRevogados.EstaRevogado(jti))
                {
                    context.Fail("Sessão encerrada.");
                    return;
                }

                var idTexto = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (!int.TryParse(idTexto, out var id))
                {
                    context.Fail("Token sem ID de usuário.");
                    return;
                }

                // Usuário desativado ou com perfil alterado perde o acesso na hora (RN04).
                var repositorio = context.HttpContext.RequestServices.GetRequiredService<IUsuarioRepository>();
                var usuario = await repositorio.BuscarPorId(id);
                if (usuario is null || !usuario.Ativo || context.Principal?.FindFirstValue(ClaimTypes.Role) != usuario.TipoUsuario)
                    context.Fail("Usuário inativo, inexistente ou com permissão alterada.");
            }
        };
    });

builder.Services.AddAuthorization();

// ---------- Swagger ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CatalogoMusica.WebAPI",
        Version = "v1",
        Description = "API do Catálogo de Música: músicas, artistas, gêneros, usuários, playlists e fila de reprodução."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Faça login em POST /api/Login e cole aqui apenas o token."
    });

    options.OperationFilter<SwaggerAuthorizeOperationFilter>();
});

// ---------- CORS (front-end) ----------
var origens = builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontEnd", policy =>
    {
        policy.WithOrigins(origens)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

await AdminInicial.ConfigurarSenhaAsync(app);

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CatalogoMusica.WebAPI v1");
        options.RoutePrefix = string.Empty;
    });
    app.MapGet("/swagger", () => Results.Redirect("/")).ExcludeFromDescription();
}

app.UseHttpsRedirection();
app.UseCors("FrontEnd");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
