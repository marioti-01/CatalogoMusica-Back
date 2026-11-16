# CatalogoMusica.WebAPI

API REST do Catálogo de Música (ASP.NET Core 10 + EF Core + SQL Server + JWT).

## Como rodar

1. Rode `CatalogoMusica.sql` no SQL Server (cria o banco `CatalogoMusica` com dados de exemplo).
2. Confira a connection string em `CatalogoMusica.WebAPI/appsettings.json`.
3. (Opcional) Preencha a seção `Cloudinary` para enviar capas das músicas.
4. Rode o projeto. O Swagger abre na raiz (`https://localhost:7091`).
5. Login inicial: `admin@catalogo.com` / `Admin@123` (definido em `AdminInicial:Senha`).
   Troque a senha depois em `PUT /api/Usuario/perfil`.

Todas as rotas exigem token, exceto o login e o cadastro público. No Swagger, clique em **Authorize** e cole o token.

## Endpoints

| Método | Rota | Quem | Requisito |
|---|---|---|---|
| POST | /api/Login | público | RF01 |
| POST | /api/Login/sair | logado | RF02 |
| GET | /api/Musica?pagina=&tamanhoPagina=&busca=&idGenero= | logado | RF03, RF04, RF05 |
| GET | /api/Musica/{id} | logado | RF06 |
| POST / PUT | /api/Musica, /api/Musica/{id} (multipart) | Admin | RF08, RF09 |
| DELETE | /api/Musica/{id} | Admin | RF10, RN22 |
| GET | /api/Genero, /api/Genero/{id} | logado | — |
| POST / PUT | /api/Genero, /api/Genero/{id} | Admin | RF11 |
| GET | /api/Artista?busca=, /api/Artista/{id} | logado | — |
| POST / PUT | /api/Artista, /api/Artista/{id} | Admin | RF16 |
| GET / PUT | /api/Usuario/perfil | logado | RF07 |
| GET | /api/Usuario | Admin | RF12 |
| GET | /api/Usuario/{id} | Admin ou o próprio | — |
| POST | /api/Usuario/cadastro | público (cria conta Comum) | — |
| POST | /api/Usuario | Admin | RF13 |
| PUT | /api/Usuario/{id} | Admin | RF14 |
| PATCH | /api/Usuario/{id}/desativar, /api/Usuario/{id}/ativar | Admin | RF15 |
| GET | /api/Playlist | dono | RF18 |
| GET | /api/Playlist/{id} | dono | RF19 |
| POST | /api/Playlist | logado | RF17 |
| PUT | /api/Playlist/{id} | dono | RF20 |
| DELETE | /api/Playlist/{id} | dono | RF21 |
| POST | /api/Playlist/{id}/musicas `{ "idMusica": 1 }` | dono | RF22 |
| DELETE | /api/Playlist/{id}/musicas/{idMusica} | dono | RF23 |
| PUT | /api/Playlist/{id}/musicas/ordem `{ "ids": [idMusica, ...] }` | dono | RF24 |
| GET | /api/Fila | logado | RF27 |
| POST | /api/Fila `{ "idMusica": 1 }` | logado | RF25 |
| POST | /api/Fila/playlist/{idPlaylist} | logado | RF26 |
| PUT | /api/Fila/ordem `{ "ids": [idFila, ...] }` | logado | RF28 |
| DELETE | /api/Fila/{idFila} | logado | RF29 |
| DELETE | /api/Fila | logado | RF30 |
| POST | /api/Fila/avancar | logado | RF31 |

Erros voltam como `{ "mensagem": "..." }` (ou a lista de erros de validação do ASP.NET em caso de 400 por campo inválido).

## Observações

- Duração da música é em **segundos**. Datas no formato `AAAA-MM-DD`.
- O logout guarda o token encerrado em memória; se a API reiniciar, ele expira normalmente pelo `Jwt:ExpirationMinutes`.
- Não guarde segredos reais (Cloudinary, senha do banco) no Git: use `dotnet user-secrets` em produção.
