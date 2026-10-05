using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Conecta.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class InicialCategoriasESuporte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descricao = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    icone_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias", x => x.id);
                    table.CheckConstraint("ck_categorias_slug", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    senha_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    perfil = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    documento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    verificado = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    foto_perfil_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    media_avaliacao = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true, defaultValue: 0m),
                    total_avaliacoes = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    termos_aceitos_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prestadores",
                columns: table => new
                {
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    nome_fantasia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    bio = table.Column<string>(type: "text", nullable: true),
                    plano_assinatura = table.Column<string>(type: "character(1)", nullable: false, defaultValue: "G"),
                    status_disponibilidade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "DISPONIVEL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prestadores", x => x.usuario_id);
                    table.ForeignKey(
                        name: "FK_prestadores_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prestador_categorias",
                columns: table => new
                {
                    prestador_id = table.Column<int>(type: "integer", nullable: false),
                    categoria_id = table.Column<int>(type: "integer", nullable: false),
                    adicionado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prestador_categorias", x => new { x.prestador_id, x.categoria_id });
                    table.ForeignKey(
                        name: "FK_prestador_categorias_categorias_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_prestador_categorias_prestadores_prestador_id",
                        column: x => x.prestador_id,
                        principalTable: "prestadores",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "uq_categorias_slug",
                table: "categorias",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prestador_categorias_categoria_id",
                table: "prestador_categorias",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prestador_categorias");

            migrationBuilder.DropTable(
                name: "categorias");

            migrationBuilder.DropTable(
                name: "prestadores");

            migrationBuilder.DropTable(
                name: "usuarios");
        }
    }
}
