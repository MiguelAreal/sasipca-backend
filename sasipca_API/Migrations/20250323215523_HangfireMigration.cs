using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sasipca_API.Migrations
{
    /// <inheritdoc />
    public partial class HangfireMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categorias_produtos",
                columns: table => new
                {
                    id_categoria = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nome = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, comment: "Nome da categoria.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categoria", x => x.id_categoria);
                });

            migrationBuilder.CreateTable(
                name: "codigo_postal",
                columns: table => new
                {
                    id_cod_postal = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: false, comment: "O próprio código postal"),
                    localidade = table.Column<string>(type: "varchar(75)", unicode: false, maxLength: 75, nullable: false, comment: "Localidade ao qual o código-postal corresponde.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_codigo_postal", x => x.id_cod_postal);
                });

            migrationBuilder.CreateTable(
                name: "estado_evento",
                columns: table => new
                {
                    id_estado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tipo_estado = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, comment: "Tipo de estado de um evento.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estado_evento", x => x.id_estado);
                });

            migrationBuilder.CreateTable(
                name: "estado_produto",
                columns: table => new
                {
                    id_estado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tipo_estado = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, comment: "Tipo de estado de um produto.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estado_produto", x => x.id_estado);
                });

            migrationBuilder.CreateTable(
                name: "estado_proposta",
                columns: table => new
                {
                    id_estado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tipo_estado = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, comment: "Descrição do estado da proposta.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proposta_estado", x => x.id_estado);
                });

            migrationBuilder.CreateTable(
                name: "estado_servico",
                columns: table => new
                {
                    id_estado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tipo_estado = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, comment: "Tipo de estado de um serviço.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estado_servico", x => x.id_estado);
                });

            migrationBuilder.CreateTable(
                name: "imagens",
                columns: table => new
                {
                    id_imagem = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, comment: "Link da imagem, guardado na conta de armazenamento Azure.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imagens", x => x.id_imagem);
                });

            migrationBuilder.CreateTable(
                name: "modalidade_preco",
                columns: table => new
                {
                    id_modalidade = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tipo = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, comment: "Se o pagamento de um serviço é Total/Hora/Outros.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modalidade_preco", x => x.id_modalidade);
                });

            migrationBuilder.CreateTable(
                name: "pessoa",
                columns: table => new
                {
                    id_pessoa = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nome = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, comment: "Nome da pessoa."),
                    morada = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, comment: "Morada da pessoa."),
                    email = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false, comment: "Endereço de E-Mail da pessoa."),
                    password = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false, comment: "Palavra-Passe encriptada por SHA-256 da pessoa."),
                    contacto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, comment: "Contacto Telefónico da pessoa."),
                    id_cod_postal = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: true, comment: "Código Postal da pessoa.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilizadores", x => x.id_pessoa);
                    table.ForeignKey(
                        name: "FK_pessoa_codpostal",
                        column: x => x.id_cod_postal,
                        principalTable: "codigo_postal",
                        principalColumn: "id_cod_postal");
                },
                comment: "Tabela para utilizadores.");

            migrationBuilder.CreateTable(
                name: "avaliacao",
                columns: table => new
                {
                    id_avaliacao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_utilizador = table.Column<int>(type: "int", nullable: false, comment: "Data/Hora que a avaliação foi criada."),
                    nota = table.Column<int>(type: "int", nullable: false, comment: "Utilizador que fez a avaliação."),
                    data_avaliacao = table.Column<DateTime>(type: "datetime", nullable: false, comment: "Nota dada a um serviço de 1 a 5."),
                    IdUtilizadorNavigationIdPessoa = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_avaliacao", x => x.id_avaliacao);
                    table.ForeignKey(
                        name: "FK_avaliacao_pessoa_IdUtilizadorNavigationIdPessoa",
                        column: x => x.IdUtilizadorNavigationIdPessoa,
                        principalTable: "pessoa",
                        principalColumn: "id_pessoa",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evento",
                columns: table => new
                {
                    id_evento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nome = table.Column<string>(type: "varchar(75)", unicode: false, maxLength: 75, nullable: false, comment: "Nome/Título do Evento"),
                    morada = table.Column<string>(type: "varchar(75)", unicode: false, maxLength: 75, nullable: false, comment: "Localização onde o evento decorre"),
                    num_min_pessoas = table.Column<int>(type: "int", nullable: false, comment: "Requisito de número mínimo de pessoas para o evento ocorrer"),
                    descricao = table.Column<string>(type: "varchar(400)", unicode: false, maxLength: 400, nullable: true, comment: "Descrição do evento"),
                    data_ini = table.Column<DateTime>(type: "datetime", nullable: false, comment: "Data/Hora de início do evento."),
                    id_estado = table.Column<int>(type: "int", nullable: true, comment: "ID da pessoa que criou o evento"),
                    id_criador = table.Column<int>(type: "int", nullable: false, comment: "ID do estado do momento do evento.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evento", x => x.id_evento);
                    table.ForeignKey(
                        name: "FK_evento_estado",
                        column: x => x.id_estado,
                        principalTable: "estado_evento",
                        principalColumn: "id_estado",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_evento_pessoa",
                        column: x => x.id_criador,
                        principalTable: "pessoa",
                        principalColumn: "id_pessoa",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "produto",
                columns: table => new
                {
                    id_produto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nome = table.Column<string>(type: "varchar(75)", unicode: false, maxLength: 75, nullable: false, comment: "Nome de Produto."),
                    preco = table.Column<decimal>(type: "decimal(10,2)", nullable: false, comment: "Preço do produto."),
                    descricao = table.Column<string>(type: "varchar(400)", unicode: false, maxLength: 400, nullable: true, comment: "Descrição de produto."),
                    id_vendedor = table.Column<int>(type: "int", nullable: false, comment: "ID da pessoa que fez o anúncio / está a vender"),
                    id_categoria = table.Column<int>(type: "int", nullable: false, comment: "ID da categoria ao qual este produto pertence."),
                    id_estado = table.Column<int>(type: "int", nullable: true, comment: "ID Referente ao estado atual do produto")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produto", x => x.id_produto);
                    table.ForeignKey(
                        name: "FK_produto_categorias",
                        column: x => x.id_categoria,
                        principalTable: "categorias_produtos",
                        principalColumn: "id_categoria");
                    table.ForeignKey(
                        name: "FK_produto_estado",
                        column: x => x.id_estado,
                        principalTable: "estado_produto",
                        principalColumn: "id_estado",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_produto_vendedor",
                        column: x => x.id_vendedor,
                        principalTable: "pessoa",
                        principalColumn: "id_pessoa");
                });

            migrationBuilder.CreateTable(
                name: "servico",
                columns: table => new
                {
                    id_servico = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nome = table.Column<string>(type: "varchar(75)", unicode: false, maxLength: 75, nullable: false, comment: "Nome/Título do Serviço"),
                    descricao = table.Column<string>(type: "varchar(400)", unicode: false, maxLength: 400, nullable: true, comment: "Descrição do Serviço"),
                    data_ini = table.Column<DateTime>(type: "datetime", nullable: false, comment: "Data/Hora para o Início do Serviço"),
                    data_fim = table.Column<DateTime>(type: "datetime", nullable: true, comment: "Data/Hora de fim do Serviço (opcional)"),
                    preco = table.Column<decimal>(type: "decimal(10,2)", nullable: false, comment: "Preço do Serviço (Por Hora/Total)."),
                    id_criador = table.Column<int>(type: "int", nullable: false, comment: "ID da pessoa que criou o serviço."),
                    id_executor = table.Column<int>(type: "int", nullable: true, comment: "ID da pessoa que executou o serviço. Nulo porque quando o registo é criado ainda não existe propostas."),
                    id_avaliacao = table.Column<int>(type: "int", nullable: true, comment: "ID da avaliação dada a este serviço."),
                    id_estado = table.Column<int>(type: "int", nullable: true, comment: "ID do estado no qual o serviço se encontra."),
                    id_modalidadepreco = table.Column<int>(type: "int", nullable: false, comment: "ID do tipo da modalidade de pagamento deste serviço (Hora/Totall)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servico", x => x.id_servico);
                    table.ForeignKey(
                        name: "FK_servico_avaliacao",
                        column: x => x.id_avaliacao,
                        principalTable: "avaliacao",
                        principalColumn: "id_avaliacao",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_servico_criador",
                        column: x => x.id_criador,
                        principalTable: "pessoa",
                        principalColumn: "id_pessoa",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_servico_estado_servico",
                        column: x => x.id_estado,
                        principalTable: "estado_servico",
                        principalColumn: "id_estado",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_servico_executor",
                        column: x => x.id_executor,
                        principalTable: "pessoa",
                        principalColumn: "id_pessoa");
                    table.ForeignKey(
                        name: "FK_servico_modalidade_preco",
                        column: x => x.id_modalidadepreco,
                        principalTable: "modalidade_preco",
                        principalColumn: "id_modalidade");
                });

            migrationBuilder.CreateTable(
                name: "item_necessario_evento",
                columns: table => new
                {
                    id_item = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_evento = table.Column<int>(type: "int", nullable: false, comment: "Evento ao qual este item pertence."),
                    nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Nome do item."),
                    quantidade = table.Column<int>(type: "int", nullable: true, comment: "Quantidade necessária deste item para este evento.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_necessario_evento", x => x.id_item);
                    table.ForeignKey(
                        name: "FK_item_necessario_evento",
                        column: x => x.id_evento,
                        principalTable: "evento",
                        principalColumn: "id_evento");
                });

            migrationBuilder.CreateTable(
                name: "imagem_produto",
                columns: table => new
                {
                    id_imagem = table.Column<int>(type: "int", nullable: false, comment: "ID da Imagem."),
                    id_produto = table.Column<int>(type: "int", nullable: false, comment: "ID do Produto ao qual a imagem reflete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imagem_produto", x => new { x.id_imagem, x.id_produto });
                    table.ForeignKey(
                        name: "FK_imagem_produto",
                        column: x => x.id_produto,
                        principalTable: "produto",
                        principalColumn: "id_produto",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_produto_imagens",
                        column: x => x.id_imagem,
                        principalTable: "imagens",
                        principalColumn: "id_imagem",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "proposta_produto",
                columns: table => new
                {
                    id_proposta_produto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    valor = table.Column<decimal>(type: "decimal(10,2)", nullable: false, comment: "Valor dado pelo comprador."),
                    id_comprador = table.Column<int>(type: "int", nullable: false, comment: "ID da pessoa a fazer a proposta."),
                    id_produto = table.Column<int>(type: "int", nullable: false, comment: "ID do produto a ter proposta"),
                    id_estado = table.Column<int>(type: "int", nullable: false, comment: "ID do estado atual da proposta.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proposta_produto", x => x.id_proposta_produto);
                    table.ForeignKey(
                        name: "FK_proposta_comprador",
                        column: x => x.id_comprador,
                        principalTable: "pessoa",
                        principalColumn: "id_pessoa",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_proposta_estado",
                        column: x => x.id_estado,
                        principalTable: "estado_proposta",
                        principalColumn: "id_estado");
                    table.ForeignKey(
                        name: "FK_proposta_produto",
                        column: x => x.id_produto,
                        principalTable: "produto",
                        principalColumn: "id_produto",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "imagem_servico",
                columns: table => new
                {
                    id_imagem = table.Column<int>(type: "int", nullable: false, comment: "ID da imagem"),
                    id_servico = table.Column<int>(type: "int", nullable: false, comment: "ID do Serviço ao qual a imagem corresponde")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imagem_servico", x => new { x.id_imagem, x.id_servico });
                    table.ForeignKey(
                        name: "FK_imagem_servico",
                        column: x => x.id_servico,
                        principalTable: "servico",
                        principalColumn: "id_servico",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_servico_imagens",
                        column: x => x.id_imagem,
                        principalTable: "imagens",
                        principalColumn: "id_imagem",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "proposta_servico",
                columns: table => new
                {
                    id_proposta_servico = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_executor = table.Column<int>(type: "int", nullable: false, comment: "ID da pessoa a fazer a proposta."),
                    id_servico = table.Column<int>(type: "int", nullable: false, comment: "ID do serviço a ter proposta"),
                    id_estado = table.Column<int>(type: "int", nullable: false, comment: "ID do estado atual da proposta.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proposta_servico", x => x.id_proposta_servico);
                    table.ForeignKey(
                        name: "FK_proposta_estado",
                        column: x => x.id_estado,
                        principalTable: "estado_proposta",
                        principalColumn: "id_estado");
                    table.ForeignKey(
                        name: "FK_proposta_executor",
                        column: x => x.id_executor,
                        principalTable: "pessoa",
                        principalColumn: "id_pessoa",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_proposta_servico",
                        column: x => x.id_servico,
                        principalTable: "servico",
                        principalColumn: "id_servico",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inscricao_evento",
                columns: table => new
                {
                    id_inscricao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_evento = table.Column<int>(type: "int", nullable: false, comment: "ID do evento ao qual a pessoa se inscreve."),
                    id_pessoa = table.Column<int>(type: "int", nullable: false, comment: "ID da pessoa à qual esta inscrição se aplica"),
                    id_item = table.Column<int>(type: "int", nullable: false, comment: "ID do item selecionado para a inscrição."),
                    data_inscricao = table.Column<DateTime>(type: "datetime", nullable: false, comment: "Data/Hora da inscrição a este evento.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inscricao_evento", x => x.id_inscricao);
                    table.ForeignKey(
                        name: "FK_inscricao_evento",
                        column: x => x.id_evento,
                        principalTable: "evento",
                        principalColumn: "id_evento",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inscricao_item",
                        column: x => x.id_item,
                        principalTable: "item_necessario_evento",
                        principalColumn: "id_item");
                    table.ForeignKey(
                        name: "FK_inscricao_pessoa",
                        column: x => x.id_pessoa,
                        principalTable: "pessoa",
                        principalColumn: "id_pessoa");
                },
                comment: "Registos de inscrição de pessoas a eventos, especificando também o item que selecionou para levar.");

            migrationBuilder.CreateIndex(
                name: "IX_avaliacao_IdUtilizadorNavigationIdPessoa",
                table: "avaliacao",
                column: "IdUtilizadorNavigationIdPessoa");

            migrationBuilder.CreateIndex(
                name: "IX_evento_id_criador",
                table: "evento",
                column: "id_criador");

            migrationBuilder.CreateIndex(
                name: "IX_evento_id_estado",
                table: "evento",
                column: "id_estado");

            migrationBuilder.CreateIndex(
                name: "IX_imagem_produto_id_produto",
                table: "imagem_produto",
                column: "id_produto");

            migrationBuilder.CreateIndex(
                name: "IX_imagem_servico_id_servico",
                table: "imagem_servico",
                column: "id_servico");

            migrationBuilder.CreateIndex(
                name: "IX_inscricao_evento_id_evento",
                table: "inscricao_evento",
                column: "id_evento");

            migrationBuilder.CreateIndex(
                name: "IX_inscricao_evento_id_item",
                table: "inscricao_evento",
                column: "id_item");

            migrationBuilder.CreateIndex(
                name: "uc_pessoa_evento",
                table: "inscricao_evento",
                columns: new[] { "id_pessoa", "id_evento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_necessario_evento_id_evento",
                table: "item_necessario_evento",
                column: "id_evento");

            migrationBuilder.CreateIndex(
                name: "IDX_Pessoa_Email",
                table: "pessoa",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_pessoa_id_cod_postal",
                table: "pessoa",
                column: "id_cod_postal");

            migrationBuilder.CreateIndex(
                name: "UQ_email",
                table: "pessoa",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_produto_id_categoria",
                table: "produto",
                column: "id_categoria");

            migrationBuilder.CreateIndex(
                name: "IX_produto_id_estado",
                table: "produto",
                column: "id_estado");

            migrationBuilder.CreateIndex(
                name: "IX_produto_id_vendedor",
                table: "produto",
                column: "id_vendedor");

            migrationBuilder.CreateIndex(
                name: "IX_proposta_produto_id_comprador",
                table: "proposta_produto",
                column: "id_comprador");

            migrationBuilder.CreateIndex(
                name: "IX_proposta_produto_id_estado",
                table: "proposta_produto",
                column: "id_estado");

            migrationBuilder.CreateIndex(
                name: "IX_proposta_produto_id_produto",
                table: "proposta_produto",
                column: "id_produto");

            migrationBuilder.CreateIndex(
                name: "IX_proposta_servico_id_estado",
                table: "proposta_servico",
                column: "id_estado");

            migrationBuilder.CreateIndex(
                name: "IX_proposta_servico_id_executor",
                table: "proposta_servico",
                column: "id_executor");

            migrationBuilder.CreateIndex(
                name: "IX_proposta_servico_id_servico",
                table: "proposta_servico",
                column: "id_servico");

            migrationBuilder.CreateIndex(
                name: "IX_servico_id_avaliacao",
                table: "servico",
                column: "id_avaliacao");

            migrationBuilder.CreateIndex(
                name: "IX_servico_id_criador",
                table: "servico",
                column: "id_criador");

            migrationBuilder.CreateIndex(
                name: "IX_servico_id_estado",
                table: "servico",
                column: "id_estado");

            migrationBuilder.CreateIndex(
                name: "IX_servico_id_executor",
                table: "servico",
                column: "id_executor");

            migrationBuilder.CreateIndex(
                name: "IX_servico_id_modalidadepreco",
                table: "servico",
                column: "id_modalidadepreco");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "imagem_produto");

            migrationBuilder.DropTable(
                name: "imagem_servico");

            migrationBuilder.DropTable(
                name: "inscricao_evento");

            migrationBuilder.DropTable(
                name: "proposta_produto");

            migrationBuilder.DropTable(
                name: "proposta_servico");

            migrationBuilder.DropTable(
                name: "imagens");

            migrationBuilder.DropTable(
                name: "item_necessario_evento");

            migrationBuilder.DropTable(
                name: "produto");

            migrationBuilder.DropTable(
                name: "estado_proposta");

            migrationBuilder.DropTable(
                name: "servico");

            migrationBuilder.DropTable(
                name: "evento");

            migrationBuilder.DropTable(
                name: "categorias_produtos");

            migrationBuilder.DropTable(
                name: "estado_produto");

            migrationBuilder.DropTable(
                name: "avaliacao");

            migrationBuilder.DropTable(
                name: "estado_servico");

            migrationBuilder.DropTable(
                name: "modalidade_preco");

            migrationBuilder.DropTable(
                name: "estado_evento");

            migrationBuilder.DropTable(
                name: "pessoa");

            migrationBuilder.DropTable(
                name: "codigo_postal");
        }
    }
}
