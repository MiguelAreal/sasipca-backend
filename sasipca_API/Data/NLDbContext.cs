using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using sasipca_API.Models;

namespace sasipca_API.Data;

public partial class NLDbContext : DbContext
{
    public NLDbContext(DbContextOptions<NLDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Avaliacao> Avaliacao { get; set; }

    public virtual DbSet<CategoriasProdutos> CategoriasProdutos { get; set; }

    public virtual DbSet<CodigoPostal> CodigoPostal { get; set; }

    public virtual DbSet<EstadoEvento> EstadoEvento { get; set; }

    public virtual DbSet<EstadoProduto> EstadoProduto { get; set; }

    public virtual DbSet<EstadoProposta> EstadoProposta { get; set; }

    public virtual DbSet<EstadoServico> EstadoServico { get; set; }

    public virtual DbSet<Evento> Evento { get; set; }

    public virtual DbSet<Imagens> Imagens { get; set; }

    public virtual DbSet<InscricaoEvento> InscricaoEvento { get; set; }

    public virtual DbSet<ItemNecessarioEvento> ItemNecessarioEvento { get; set; }

    public virtual DbSet<ModalidadePreco> ModalidadePreco { get; set; }

    public virtual DbSet<Notificacao> Notificacao { get; set; }

    public virtual DbSet<Pessoa> Pessoa { get; set; }

    public virtual DbSet<Produto> Produto { get; set; }

    public virtual DbSet<PropostaProduto> PropostaProduto { get; set; }

    public virtual DbSet<PropostaServico> PropostaServico { get; set; }

    public virtual DbSet<Servico> Servico { get; set; }

    //public virtual DbSet<TokenResetPassword> TokenResetPassword { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Avaliacao>(entity =>
        {
            entity.HasKey(e => e.IdAvaliacao);

            entity.ToTable("avaliacao");

            entity.Property(e => e.IdAvaliacao).HasColumnName("id_avaliacao");
            entity.Property(e => e.DataAvaliacao)
                .HasComment("Nota dada a um serviço de 1 a 5.")
                .HasColumnType("datetime")
                .HasColumnName("data_avaliacao");
            entity.Property(e => e.IdUtilizador)
                .HasComment("Data/Hora que a avaliação foi criada.")
                .HasColumnName("id_utilizador");
            entity.Property(e => e.Nota)
                .HasComment("Utilizador que fez a avaliação.")
                .HasColumnName("nota");

            entity.HasOne(d => d.IdUtilizadorNavigation).WithMany(p => p.Avaliacao)
                .HasForeignKey(d => d.IdUtilizador)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_avaliacao_pessoa");
        });

        modelBuilder.Entity<CategoriasProdutos>(entity =>
        {
            entity.HasKey(e => e.IdCategoria).HasName("PK_categoria");

            entity.ToTable("categorias_produtos");

            entity.Property(e => e.IdCategoria).HasColumnName("id_categoria");
            entity.Property(e => e.Nome)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("Nome da categoria.")
                .HasColumnName("nome");
        });

        modelBuilder.Entity<CodigoPostal>(entity =>
        {
            entity.HasKey(e => e.IdCodPostal);

            entity.ToTable("codigo_postal");

            entity.Property(e => e.IdCodPostal)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasComment("O próprio código postal")
                .HasColumnName("id_cod_postal");
            entity.Property(e => e.Localidade)
                .HasMaxLength(75)
                .IsUnicode(false)
                .HasComment("Localidade ao qual o código-postal corresponde.")
                .HasColumnName("localidade");
        });

        modelBuilder.Entity<EstadoEvento>(entity =>
        {
            entity.HasKey(e => e.IdEstado);

            entity.ToTable("estado_evento");

            entity.Property(e => e.IdEstado).HasColumnName("id_estado");
            entity.Property(e => e.TipoEstado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("Tipo de estado de um evento.")
                .HasColumnName("tipo_estado");
        });

        modelBuilder.Entity<EstadoProduto>(entity =>
        {
            entity.HasKey(e => e.IdEstado);

            entity.ToTable("estado_produto");

            entity.Property(e => e.IdEstado).HasColumnName("id_estado");
            entity.Property(e => e.TipoEstado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("Tipo de estado de um produto.")
                .HasColumnName("tipo_estado");
        });

        modelBuilder.Entity<EstadoProposta>(entity =>
        {
            entity.HasKey(e => e.IdEstado).HasName("PK_proposta_estado");

            entity.ToTable("estado_proposta");

            entity.Property(e => e.IdEstado).HasColumnName("id_estado");
            entity.Property(e => e.TipoEstado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("Descrição do estado da proposta.")
                .HasColumnName("tipo_estado");
        });

        modelBuilder.Entity<EstadoServico>(entity =>
        {
            entity.HasKey(e => e.IdEstado);

            entity.ToTable("estado_servico");

            entity.Property(e => e.IdEstado).HasColumnName("id_estado");
            entity.Property(e => e.TipoEstado)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("Tipo de estado de um serviço.")
                .HasColumnName("tipo_estado");
        });

        modelBuilder.Entity<Evento>(entity =>
        {
            entity.HasKey(e => e.IdEvento).HasName("PK_Evento");

            entity.ToTable("evento");

            entity.Property(e => e.IdEvento).HasColumnName("id_evento");
            entity.Property(e => e.DataCriacao)
                .HasColumnType("datetime")
                .HasColumnName("data_criacao");
            entity.Property(e => e.DataIni)
                .HasComment("Data/Hora de início do evento.")
                .HasColumnType("datetime")
                .HasColumnName("data_ini");
            entity.Property(e => e.Descricao)
                .HasMaxLength(400)
                .IsUnicode(false)
                .HasComment("Descrição do evento")
                .HasColumnName("descricao");
            entity.Property(e => e.IdCriador)
                .HasComment("ID do estado do momento do evento.")
                .HasColumnName("id_criador");
            entity.Property(e => e.IdEstado)
                .HasComment("ID da pessoa que criou o evento")
                .HasColumnName("id_estado");
            entity.Property(e => e.Morada)
                .HasMaxLength(75)
                .IsUnicode(false)
                .HasComment("Localização onde o evento decorre")
                .HasColumnName("morada");
            entity.Property(e => e.Nome)
                .HasMaxLength(75)
                .IsUnicode(false)
                .HasComment("Nome/Título do Evento")
                .HasColumnName("nome");
            entity.Property(e => e.NumMinPessoas)
                .HasComment("Requisito de número mínimo de pessoas para o evento ocorrer")
                .HasColumnName("num_min_pessoas");

            entity.HasOne(d => d.IdCriadorNavigation).WithMany(p => p.Evento)
                .HasForeignKey(d => d.IdCriador)
                .HasConstraintName("FK_evento_pessoa");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Evento)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_evento_estado");
        });

        modelBuilder.Entity<Imagens>(entity =>
        {
            entity.HasKey(e => e.IdImagem);

            entity.ToTable("imagens");

            entity.Property(e => e.IdImagem).HasColumnName("id_imagem");
            entity.Property(e => e.Url)
                .HasMaxLength(500)
                .HasComment("Link da imagem, guardado na conta de armazenamento Azure.")
                .HasColumnName("url");

            entity.HasMany(d => d.IdProduto).WithMany(p => p.IdImagem)
                .UsingEntity<Dictionary<string, object>>(
                    "ImagemProduto",
                    r => r.HasOne<Produto>().WithMany()
                        .HasForeignKey("IdProduto")
                        .HasConstraintName("FK_imagem_produto"),
                    l => l.HasOne<Imagens>().WithMany()
                        .HasForeignKey("IdImagem")
                        .HasConstraintName("FK_produto_imagens"),
                    j =>
                    {
                        j.HasKey("IdImagem", "IdProduto");
                        j.ToTable("imagem_produto");
                        j.IndexerProperty<int>("IdImagem")
                            .HasComment("ID da Imagem.")
                            .HasColumnName("id_imagem");
                        j.IndexerProperty<int>("IdProduto")
                            .HasComment("ID do Produto ao qual a imagem reflete.")
                            .HasColumnName("id_produto");
                    });

            entity.HasMany(d => d.IdServico).WithMany(p => p.IdImagem)
                .UsingEntity<Dictionary<string, object>>(
                    "ImagemServico",
                    r => r.HasOne<Servico>().WithMany()
                        .HasForeignKey("IdServico")
                        .HasConstraintName("FK_imagem_servico"),
                    l => l.HasOne<Imagens>().WithMany()
                        .HasForeignKey("IdImagem")
                        .HasConstraintName("FK_servico_imagens"),
                    j =>
                    {
                        j.HasKey("IdImagem", "IdServico");
                        j.ToTable("imagem_servico");
                        j.IndexerProperty<int>("IdImagem")
                            .HasComment("ID da imagem")
                            .HasColumnName("id_imagem");
                        j.IndexerProperty<int>("IdServico")
                            .HasComment("ID do Serviço ao qual a imagem corresponde")
                            .HasColumnName("id_servico");
                    });
        });

        modelBuilder.Entity<InscricaoEvento>(entity =>
        {
            entity.HasKey(e => e.IdInscricao);

            entity.ToTable("inscricao_evento", tb => tb.HasComment("Registos de inscrição de pessoas a eventos, especificando também o item que selecionou para levar."));

            entity.HasIndex(e => new { e.IdPessoa, e.IdEvento }, "uc_pessoa_evento").IsUnique();

            entity.Property(e => e.IdInscricao).HasColumnName("id_inscricao");
            entity.Property(e => e.DataInscricao)
                .HasComment("Data/Hora da inscrição a este evento.")
                .HasColumnType("datetime")
                .HasColumnName("data_inscricao");
            entity.Property(e => e.IdEvento)
                .HasComment("ID do evento ao qual a pessoa se inscreve.")
                .HasColumnName("id_evento");
            entity.Property(e => e.IdItem)
                .HasComment("ID do item selecionado para a inscrição.")
                .HasColumnName("id_item");
            entity.Property(e => e.IdPessoa)
                .HasComment("ID da pessoa à qual esta inscrição se aplica")
                .HasColumnName("id_pessoa");

            entity.HasOne(d => d.IdEventoNavigation).WithMany(p => p.InscricaoEvento)
                .HasForeignKey(d => d.IdEvento)
                .HasConstraintName("FK_inscricao_evento");

            entity.HasOne(d => d.IdItemNavigation).WithMany(p => p.InscricaoEvento)
                .HasForeignKey(d => d.IdItem)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_inscricao_item");

            entity.HasOne(d => d.IdPessoaNavigation).WithMany(p => p.InscricaoEvento)
                .HasForeignKey(d => d.IdPessoa)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_inscricao_pessoa");
        });

        modelBuilder.Entity<ItemNecessarioEvento>(entity =>
        {
            entity.HasKey(e => e.IdItem);

            entity.ToTable("item_necessario_evento");

            entity.Property(e => e.IdItem).HasColumnName("id_item");
            entity.Property(e => e.IdEvento)
                .HasComment("Evento ao qual este item pertence.")
                .HasColumnName("id_evento");
            entity.Property(e => e.Nome)
                .HasMaxLength(100)
                .HasComment("Nome do item.")
                .HasColumnName("nome");
            entity.Property(e => e.Quantidade)
                .HasComment("Quantidade necessária deste item para este evento.")
                .HasColumnName("quantidade");

            entity.HasOne(d => d.IdEventoNavigation).WithMany(p => p.ItemNecessarioEvento)
                .HasForeignKey(d => d.IdEvento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_item_necessario_evento");
        });

        modelBuilder.Entity<ModalidadePreco>(entity =>
        {
            entity.HasKey(e => e.IdModalidade);

            entity.ToTable("modalidade_preco");

            entity.Property(e => e.IdModalidade).HasColumnName("id_modalidade");
            entity.Property(e => e.Tipo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasComment("Se o pagamento de um serviço é Total/Hora/Outros.")
                .HasColumnName("tipo");
        });

        modelBuilder.Entity<Notificacao>(entity =>
        {
            entity.HasKey(e => e.IdNotificacao);

            entity.ToTable("notificacao");

            entity.Property(e => e.IdNotificacao).HasColumnName("id_notificacao");
            entity.Property(e => e.DataCriacao)
                .HasColumnType("datetime")
                .HasColumnName("data_criacao");
            entity.Property(e => e.IdPessoa).HasColumnName("id_pessoa");
            entity.Property(e => e.Mensagem)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("mensagem");

            entity.HasOne(d => d.IdPessoaNavigation).WithMany(p => p.Notificacao)
                .HasForeignKey(d => d.IdPessoa)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_notificacao_pessoa");
        });

        modelBuilder.Entity<Pessoa>(entity =>
        {
            entity.HasKey(e => e.IdPessoa).HasName("PK_Utilizadores");

            entity.ToTable("pessoa", tb => tb.HasComment("Tabela para utilizadores."));

            entity.HasIndex(e => e.Email, "UQ_email").IsUnique();

            entity.Property(e => e.IdPessoa).HasColumnName("id_pessoa");
            entity.Property(e => e.Contacto)
                .HasMaxLength(20)
                .HasComment("Contacto Telefónico da pessoa.")
                .HasColumnName("contacto");
            entity.Property(e => e.DataCriacao)
                .HasColumnType("datetime")
                .HasColumnName("data_criacao");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasComment("Endereço de E-Mail da pessoa.")
                .HasColumnName("email");
            entity.Property(e => e.IdCodPostal)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasComment("Código Postal da pessoa.")
                .HasColumnName("id_cod_postal");
            entity.Property(e => e.Morada)
                .HasMaxLength(255)
                .HasComment("Morada da pessoa.")
                .HasColumnName("morada");
            entity.Property(e => e.Nome)
                .HasMaxLength(255)
                .HasComment("Nome da pessoa.")
                .HasColumnName("nome");
            entity.Property(e => e.Password)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasComment("Palavra-Passe encriptada por SHA-256 da pessoa.")
                .HasColumnName("password");
            entity.Property(e => e.RefreshToken)
                .HasMaxLength(700)
                .IsUnicode(false)
                .HasColumnName("refresh_token");
            entity.Property(e => e.RefreshTokenExpiryTime)
                .HasColumnType("datetime")
                .HasColumnName("refresh_token_expiry_time");

            entity.HasOne(d => d.IdCodPostalNavigation).WithMany(p => p.Pessoa)
                .HasForeignKey(d => d.IdCodPostal)
                .HasConstraintName("FK_pessoa_codpostal");
        });

        modelBuilder.Entity<Produto>(entity =>
        {
            entity.HasKey(e => e.IdProduto).HasName("PK_Produto");

            entity.ToTable("produto");

            entity.Property(e => e.IdProduto).HasColumnName("id_produto");
            entity.Property(e => e.DataCriacao)
                .HasColumnType("datetime")
                .HasColumnName("data_criacao");
            entity.Property(e => e.Descricao)
                .HasMaxLength(400)
                .IsUnicode(false)
                .HasColumnName("descricao");
            entity.Property(e => e.IdCategoria)
                .HasComment("ID da categoria ao qual este produto pertence.")
                .HasColumnName("id_categoria");
            entity.Property(e => e.IdEstado)
                .HasComment("ID Referente ao estado atual do produto")
                .HasColumnName("id_estado");
            entity.Property(e => e.IdVendedor)
                .HasComment("ID da pessoa que fez o anúncio / está a vender")
                .HasColumnName("id_vendedor");
            entity.Property(e => e.Nome)
                .HasMaxLength(75)
                .IsUnicode(false)
                .HasComment("Nome de Produto.")
                .HasColumnName("nome");
            entity.Property(e => e.Preco)
                .HasComment("Preço do produto.")
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("preco");

            entity.HasOne(d => d.IdCategoriaNavigation).WithMany(p => p.Produto)
                .HasForeignKey(d => d.IdCategoria)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_produto_categorias");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Produto)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_produto_estado");

            entity.HasOne(d => d.IdVendedorNavigation).WithMany(p => p.Produto)
                .HasForeignKey(d => d.IdVendedor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_produto_vendedor");
        });

        modelBuilder.Entity<PropostaProduto>(entity =>
        {
            entity.HasKey(e => e.IdPropostaProduto);

            entity.ToTable("proposta_produto");

            entity.Property(e => e.IdPropostaProduto).HasColumnName("id_proposta_produto");
            entity.Property(e => e.IdComprador)
                .HasComment("ID da pessoa a fazer a proposta.")
                .HasColumnName("id_comprador");
            entity.Property(e => e.IdEstado)
                .HasComment("ID do estado atual da proposta.")
                .HasColumnName("id_estado");
            entity.Property(e => e.IdProduto)
                .HasComment("ID do produto a ter proposta")
                .HasColumnName("id_produto");
            entity.Property(e => e.Valor)
                .HasComment("Valor dado pelo comprador.")
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("valor");

            entity.HasOne(d => d.IdCompradorNavigation).WithMany(p => p.PropostaProduto)
                .HasForeignKey(d => d.IdComprador)
                .HasConstraintName("FK_proposta_comprador");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.PropostaProduto)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_proposta_estado");

            entity.HasOne(d => d.IdProdutoNavigation).WithMany(p => p.PropostaProduto)
                .HasForeignKey(d => d.IdProduto)
                .HasConstraintName("FK_proposta_produto");
        });

        modelBuilder.Entity<PropostaServico>(entity =>
        {
            entity.HasKey(e => e.IdPropostaServico);

            entity.ToTable("proposta_servico");

            entity.Property(e => e.IdPropostaServico).HasColumnName("id_proposta_servico");
            entity.Property(e => e.IdEstado)
                .HasComment("ID do estado atual da proposta.")
                .HasColumnName("id_estado");
            entity.Property(e => e.IdExecutor)
                .HasComment("ID da pessoa a fazer a proposta.")
                .HasColumnName("id_executor");
            entity.Property(e => e.IdServico)
                .HasComment("ID do serviço a ter proposta")
                .HasColumnName("id_servico");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.PropostaServico)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_proposta_servico_estado");

            entity.HasOne(d => d.IdExecutorNavigation).WithMany(p => p.PropostaServico)
                .HasForeignKey(d => d.IdExecutor)
                .HasConstraintName("FK_proposta_executor");

            entity.HasOne(d => d.IdServicoNavigation).WithMany(p => p.PropostaServico)
                .HasForeignKey(d => d.IdServico)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_proposta_servico");
        });

        modelBuilder.Entity<Servico>(entity =>
        {
            entity.HasKey(e => e.IdServico).HasName("PK_Servico");

            entity.ToTable("servico");

            entity.Property(e => e.IdServico).HasColumnName("id_servico");
            entity.Property(e => e.DataCriacao)
                .HasColumnType("datetime")
                .HasColumnName("data_criacao");
            entity.Property(e => e.DataFim)
                .HasComment("Data/Hora de fim do Serviço (opcional)")
                .HasColumnType("datetime")
                .HasColumnName("data_fim");
            entity.Property(e => e.DataIni)
                .HasComment("Data/Hora para o Início do Serviço")
                .HasColumnType("datetime")
                .HasColumnName("data_ini");
            entity.Property(e => e.Descricao)
                .HasMaxLength(400)
                .IsUnicode(false)
                .HasComment("Descrição do Serviço")
                .HasColumnName("descricao");
            entity.Property(e => e.IdAvaliacao)
                .HasComment("ID da avaliação dada a este serviço.")
                .HasColumnName("id_avaliacao");
            entity.Property(e => e.IdCriador)
                .HasComment("ID da pessoa que criou o serviço.")
                .HasColumnName("id_criador");
            entity.Property(e => e.IdEstado)
                .HasComment("ID do estado no qual o serviço se encontra.")
                .HasColumnName("id_estado");
            entity.Property(e => e.IdExecutor)
                .HasComment("ID da pessoa que executou o serviço. Nulo porque quando o registo é criado ainda não existe propostas.")
                .HasColumnName("id_executor");
            entity.Property(e => e.IdModalidadepreco)
                .HasComment("ID do tipo da modalidade de pagamento deste serviço (Hora/Totall)")
                .HasColumnName("id_modalidadepreco");
            entity.Property(e => e.Nome)
                .HasMaxLength(75)
                .IsUnicode(false)
                .HasComment("Nome/Título do Serviço")
                .HasColumnName("nome");
            entity.Property(e => e.Preco)
                .HasComment("Preço do Serviço (Por Hora/Total).")
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("preco");

            entity.HasOne(d => d.IdAvaliacaoNavigation).WithMany(p => p.Servico)
                .HasForeignKey(d => d.IdAvaliacao)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_servico_avaliacao");

            entity.HasOne(d => d.IdCriadorNavigation).WithMany(p => p.ServicoIdCriadorNavigation)
                .HasForeignKey(d => d.IdCriador)
                .HasConstraintName("FK_servico_criador");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Servico)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_servico_estado_servico");

            entity.HasOne(d => d.IdExecutorNavigation).WithMany(p => p.ServicoIdExecutorNavigation)
                .HasForeignKey(d => d.IdExecutor)
                .HasConstraintName("FK_servico_executor");

            entity.HasOne(d => d.IdModalidadeprecoNavigation).WithMany(p => p.Servico)
                .HasForeignKey(d => d.IdModalidadepreco)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_servico_modalidade_preco");
        });

        /*modelBuilder.Entity<TokenResetPassword>(entity =>
        {
            entity.ToTable("token_reset_password");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DataExpiracao)
                .HasColumnType("datetime")
                .HasColumnName("data_expiracao");
            entity.Property(e => e.IdPessoa).HasColumnName("id_pessoa");
            entity.Property(e => e.Token)
                .HasMaxLength(700)
                .IsUnicode(false)
                .HasColumnName("token");

            entity.HasOne(d => d.IdPessoaNavigation).WithMany(p => p.TokenResetPassword)
                .HasForeignKey(d => d.IdPessoa)
                .HasConstraintName("FK_token_pessoa");
        });*/

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
