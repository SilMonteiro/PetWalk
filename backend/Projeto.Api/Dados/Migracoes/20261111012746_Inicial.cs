using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projeto.Api.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Telefone = table.Column<string>(type: "TEXT", nullable: false),
                    TipoPerfil = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    SenhaHash = table.Column<string>(type: "TEXT", nullable: false),
                    ValorHora = table.Column<decimal>(type: "TEXT", nullable: false),
                    TipoCobranca = table.Column<string>(type: "TEXT", nullable: false),
                    MediaAvaliacao = table.Column<double>(type: "REAL", nullable: false),
                    Endereco = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Especie = table.Column<string>(type: "TEXT", nullable: false),
                    Raca = table.Column<string>(type: "TEXT", nullable: false),
                    Porte = table.Column<string>(type: "TEXT", nullable: false),
                    Idade = table.Column<int>(type: "INTEGER", nullable: false),
                    Observacoes = table.Column<string>(type: "TEXT", nullable: false),
                    TutorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Vacinas = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pets_Usuarios_TutorId",
                        column: x => x.TutorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Passeios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DataHora = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Duracao = table.Column<long>(type: "INTEGER", nullable: false),
                    Local = table.Column<string>(type: "TEXT", nullable: false),
                    Valor = table.Column<decimal>(type: "TEXT", nullable: false),
                    TutorId = table.Column<int>(type: "INTEGER", nullable: false),
                    PasseadorId = table.Column<int>(type: "INTEGER", nullable: false),
                    PetId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Passeios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Passeios_Pets_PetId",
                        column: x => x.PetId,
                        principalTable: "Pets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Passeios_Usuarios_PasseadorId",
                        column: x => x.PasseadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Passeios_Usuarios_TutorId",
                        column: x => x.TutorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Avaliacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nota = table.Column<int>(type: "INTEGER", nullable: false),
                    Comentario = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AvaliadorId = table.Column<int>(type: "INTEGER", nullable: false),
                    PasseioId = table.Column<int>(type: "INTEGER", nullable: false),
                    AvaliadoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Avaliacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Avaliacoes_Passeios_PasseioId",
                        column: x => x.PasseioId,
                        principalTable: "Passeios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Avaliacoes_Usuarios_AvaliadoId",
                        column: x => x.AvaliadoId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Avaliacoes_Usuarios_AvaliadorId",
                        column: x => x.AvaliadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notificacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DestinatarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", nullable: false),
                    Mensagem = table.Column<string>(type: "TEXT", nullable: false),
                    Lida = table.Column<bool>(type: "INTEGER", nullable: false),
                    DataEnvio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PasseioId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificacoes_Passeios_PasseioId",
                        column: x => x.PasseioId,
                        principalTable: "Passeios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notificacoes_Usuarios_DestinatarioId",
                        column: x => x.DestinatarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Avaliacoes_AvaliadoId",
                table: "Avaliacoes",
                column: "AvaliadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Avaliacoes_AvaliadorId",
                table: "Avaliacoes",
                column: "AvaliadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Avaliacoes_PasseioId_AvaliadorId",
                table: "Avaliacoes",
                columns: new[] { "PasseioId", "AvaliadorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_DestinatarioId",
                table: "Notificacoes",
                column: "DestinatarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_PasseioId",
                table: "Notificacoes",
                column: "PasseioId");

            migrationBuilder.CreateIndex(
                name: "IX_Passeios_PasseadorId",
                table: "Passeios",
                column: "PasseadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Passeios_PetId",
                table: "Passeios",
                column: "PetId");

            migrationBuilder.CreateIndex(
                name: "IX_Passeios_TutorId",
                table: "Passeios",
                column: "TutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Pets_TutorId",
                table: "Pets",
                column: "TutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Avaliacoes");

            migrationBuilder.DropTable(
                name: "Notificacoes");

            migrationBuilder.DropTable(
                name: "Passeios");

            migrationBuilder.DropTable(
                name: "Pets");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
