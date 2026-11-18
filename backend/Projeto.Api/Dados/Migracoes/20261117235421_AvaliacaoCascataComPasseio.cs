using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projeto.Api.Dados.Migracoes
{
    /// <inheritdoc />
    public partial class AvaliacaoCascataComPasseio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Avaliacoes_Passeios_PasseioId",
                table: "Avaliacoes");

            migrationBuilder.AddForeignKey(
                name: "FK_Avaliacoes_Passeios_PasseioId",
                table: "Avaliacoes",
                column: "PasseioId",
                principalTable: "Passeios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Avaliacoes_Passeios_PasseioId",
                table: "Avaliacoes");

            migrationBuilder.AddForeignKey(
                name: "FK_Avaliacoes_Passeios_PasseioId",
                table: "Avaliacoes",
                column: "PasseioId",
                principalTable: "Passeios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
