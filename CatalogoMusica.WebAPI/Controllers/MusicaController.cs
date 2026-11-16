using CatalogoMusica.WebAPI.DTO;
using CatalogoMusica.WebAPI.Interfaces;
using CatalogoMusica.WebAPI.Models;
using CatalogoMusica.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogoMusica.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MusicaController : ApiControllerBase
{
    private const int TamanhoMaximoRequisicao = 11 * 1024 * 1024;

    private readonly IMusicaRepository _musicaRepository;
    private readonly IArtistaRepository _artistaRepository;
    private readonly IGeneroRepository _generoRepository;
    private readonly IImageService _imageService;

    public MusicaController(
        IMusicaRepository musicaRepository,
        IArtistaRepository artistaRepository,
        IGeneroRepository generoRepository,
        IImageService imageService)
    {
        _musicaRepository = musicaRepository;
        _artistaRepository = artistaRepository;
        _generoRepository = generoRepository;
        _imageService = imageService;
    }

    /// <summary>
    /// Lista as músicas com paginação (RF03).
    /// "busca" procura no título ou no nome do artista (RF05); "idGenero" filtra por gênero (RF04).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 10,
        [FromQuery] string? busca = null,
        [FromQuery] int? idGenero = null)
    {
        if (pagina < 1 || tamanhoPagina is < 1 or > 100)
            return Erro(400, "A página deve ser positiva e o tamanhoPagina deve estar entre 1 e 100.");

        var (musicas, total) = await _musicaRepository.ListarPaginado(pagina, tamanhoPagina, busca, idGenero);

        return Ok(new PaginaDTO<MusicaRespostaDTO>(
            musicas.Select(MusicaRespostaDTO.Converter).ToList(), total, pagina, tamanhoPagina));
    }

    /// <summary>Detalhes de uma música (RF06).</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> BuscarPorId(int id)
    {
        var musica = await _musicaRepository.BuscarPorId(id);
        return musica is null
            ? Erro(404, "Música não encontrada.")
            : Ok(MusicaRespostaDTO.Converter(musica));
    }

    /// <summary>Cadastra uma música (RF08). Envie como multipart/form-data.</summary>
    [HttpPost]
    [Authorize(Roles = TiposUsuario.Admin)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(TamanhoMaximoRequisicao)]
    public async Task<IActionResult> Cadastrar([FromForm] MusicaDTO dto)
    {
        var validacao = await ValidarReferencias(dto);
        if (validacao.erro is not null) return validacao.erro;

        var upload = await EnviarCapa(dto.ArquivoImagem);
        if (upload.erro is not null) return upload.erro;
        var novaUrl = upload.url;

        var musica = Converter(dto, novaUrl ?? Limpar(dto.ImagemCapa));
        musica.Generos = validacao.generos;

        try
        {
            await _musicaRepository.Cadastrar(musica);
        }
        catch
        {
            if (novaUrl is not null) await _imageService.DeleteImageAsync(novaUrl);
            throw;
        }

        var criada = await _musicaRepository.BuscarPorId(musica.IdMusica);
        return CreatedAtAction(nameof(BuscarPorId), new { id = musica.IdMusica }, MusicaRespostaDTO.Converter(criada!));
    }

    /// <summary>
    /// Edita uma música (RF09). Envie como multipart/form-data.
    /// Sem ArquivoImagem/ImagemCapa a capa atual é mantida; RemoverCapa=true remove.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = TiposUsuario.Admin)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(TamanhoMaximoRequisicao)]
    public async Task<IActionResult> Atualizar(int id, [FromForm] MusicaDTO dto)
    {
        var atual = await _musicaRepository.BuscarPorId(id);
        if (atual is null) return Erro(404, "Música não encontrada.");

        var validacao = await ValidarReferencias(dto);
        if (validacao.erro is not null) return validacao.erro;

        var upload = await EnviarCapa(dto.ArquivoImagem);
        if (upload.erro is not null) return upload.erro;
        var novaUrl = upload.url;

        var imagem = novaUrl
            ?? (dto.RemoverCapa ? null : Limpar(dto.ImagemCapa) ?? atual.ImagemCapa);

        try
        {
            await _musicaRepository.Atualizar(id, Converter(dto, imagem), validacao.generos);
        }
        catch
        {
            if (novaUrl is not null) await _imageService.DeleteImageAsync(novaUrl);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(atual.ImagemCapa) && atual.ImagemCapa != imagem)
            await _imageService.DeleteImageAsync(atual.ImagemCapa);

        return NoContent();
    }

    /// <summary>
    /// Exclui uma música do catálogo (RF10). Ela sai automaticamente de todas
    /// as playlists e filas, e as posições são reorganizadas (RN22).
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = TiposUsuario.Admin)]
    public async Task<IActionResult> Excluir(int id)
    {
        var musica = await _musicaRepository.BuscarPorId(id);
        if (musica is null) return Erro(404, "Música não encontrada.");

        await _musicaRepository.Excluir(id);

        if (!string.IsNullOrWhiteSpace(musica.ImagemCapa))
            await _imageService.DeleteImageAsync(musica.ImagemCapa);

        return NoContent();
    }

    private async Task<(List<Genero> generos, IActionResult? erro)> ValidarReferencias(MusicaDTO dto)
    {
        if (!await _artistaRepository.Existe(dto.IdArtista!.Value))
            return (new List<Genero>(), Erro(400, "O artista informado não existe."));

        var idsGeneros = dto.IdGeneros.Distinct().ToArray();
        var generos = await _generoRepository.BuscarPorIds(idsGeneros);
        if (generos.Count != idsGeneros.Length)
            return (new List<Genero>(), Erro(400, "Um ou mais gêneros informados não existem."));

        return (generos, null);
    }

    private async Task<(string? url, IActionResult? erro)> EnviarCapa(IFormFile? arquivo)
    {
        if (arquivo is null) return (null, null);

        if (!_imageService.IsConfigured)
            return (null, Erro(503, "Configure as credenciais do Cloudinary para enviar imagens."));

        try
        {
            return (await _imageService.UploadImageAsync(arquivo), null);
        }
        catch (ArgumentException erro)
        {
            return (null, Erro(400, erro.Message));
        }
        catch (HttpRequestException)
        {
            return (null, Erro(502, "Falha ao enviar a capa ao Cloudinary."));
        }
    }

    private static string? Limpar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static Musica Converter(MusicaDTO dto, string? imagemCapa) => new()
    {
        Titulo = dto.Titulo.Trim(),
        IdArtista = dto.IdArtista!.Value,
        Album = Limpar(dto.Album),
        DataLancamento = dto.DataLancamento!.Value,
        Duracao = dto.Duracao!.Value,
        ImagemCapa = imagemCapa
    };
}
