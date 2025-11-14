using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace sasipca_API.DBModels;

public partial class SasipcaContext : DbContext
{
    public SasipcaContext(DbContextOptions<SasipcaContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Beneficiary> Beneficiaries { get; set; }

    public virtual DbSet<BeneficiaryAddress> BeneficiaryAddresses { get; set; }

    public virtual DbSet<Campaign> Campaigns { get; set; }

    public virtual DbSet<CategoryType> CategoryTypes { get; set; }

    public virtual DbSet<Delivery> Deliveries { get; set; }

    public virtual DbSet<DeliveryItem> DeliveryItems { get; set; }

    public virtual DbSet<DeliveryStatus> DeliveryStatuses { get; set; }

    public virtual DbSet<Movement> Movements { get; set; }

    public virtual DbSet<MovementItem> MovementItems { get; set; }

    public virtual DbSet<MovementType> MovementTypes { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<NotificationStatus> NotificationStatuses { get; set; }

    public virtual DbSet<ParticularOb> ParticularObs { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductLot> ProductLots { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<ReportType> ReportTypes { get; set; }

    public virtual DbSet<TokenResetPassword> TokenResetPasswords { get; set; }

    public virtual DbSet<UnitType> UnitTypes { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VDeliveriesDetail> VDeliveriesDetails { get; set; }

    public virtual DbSet<VDelivery> VDeliveries { get; set; }

    public virtual DbSet<VMovHistory> VMovHistories { get; set; }

    public virtual DbSet<VMovHistoryDetail> VMovHistoryDetails { get; set; }

    public virtual DbSet<VStockPerLot> VStockPerLots { get; set; }

    public virtual DbSet<VStockPerProduct> VStockPerProducts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_general_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Beneficiary>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("beneficiaries");

            entity.HasIndex(e => e.AddressId, "FK_beneficiaries_beneficiary_address");

            entity.HasIndex(e => e.CreatedBy, "FK_beneficiaries_users");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.AddressId)
                .HasColumnType("int(11)")
                .HasColumnName("address_id");
            entity.Property(e => e.Contact)
                .HasMaxLength(13)
                .HasDefaultValueSql("''")
                .HasColumnName("contact");
            entity.Property(e => e.Course)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("course");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasColumnType("int(11)")
                .HasColumnName("created_by");
            entity.Property(e => e.CurricularYear)
                .HasColumnType("int(2)")
                .HasColumnName("curricular_year");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .HasColumnName("email");
            entity.Property(e => e.GlobalObs)
                .HasColumnType("text")
                .HasColumnName("global_obs");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.Nif)
                .HasColumnType("int(9)")
                .HasColumnName("nif");
            entity.Property(e => e.StudentNum)
                .HasColumnType("int(10)")
                .HasColumnName("student_num");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Address).WithMany(p => p.Beneficiaries)
                .HasForeignKey(d => d.AddressId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_beneficiaries_beneficiary_address");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Beneficiaries)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_beneficiaries_users");
        });

        modelBuilder.Entity<BeneficiaryAddress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("beneficiary_address");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Number)
                .HasColumnType("int(11)")
                .HasColumnName("number");
            entity.Property(e => e.PostalCode)
                .HasMaxLength(9)
                .HasColumnName("postal_code");
            entity.Property(e => e.Street)
                .HasMaxLength(255)
                .HasColumnName("street");
        });

        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("campaigns");

            entity.HasIndex(e => e.UserId, "user_id_fk");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.EndDate)
                .HasColumnType("datetime")
                .HasColumnName("end_date");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(100)
                .HasColumnName("image_url");
            entity.Property(e => e.Location)
                .HasMaxLength(50)
                .HasColumnName("location");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.StartDate)
                .HasColumnType("datetime")
                .HasColumnName("start_date");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId)
                .HasColumnType("int(11)")
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Campaigns)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_id_fk");
        });

        modelBuilder.Entity<CategoryType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("category_types");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Type)
                .HasMaxLength(255)
                .HasColumnName("type");
        });

        modelBuilder.Entity<Delivery>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("deliveries");

            entity.HasIndex(e => e.StatusId, "FK_deliveries_delivery_status");

            entity.HasIndex(e => e.UserId, "FK_deliveries_users");

            entity.HasIndex(e => e.BeneficiaryId, "deliveries_fk1");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.BeneficiaryId)
                .HasColumnType("int(11)")
                .HasColumnName("beneficiary_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Note)
                .HasColumnType("text")
                .HasColumnName("note");
            entity.Property(e => e.ScheduledDate).HasColumnName("scheduled_date");
            entity.Property(e => e.StatusId)
                .HasColumnType("int(11)")
                .HasColumnName("status_id");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId)
                .HasColumnType("int(11)")
                .HasColumnName("user_id");

            entity.HasOne(d => d.Beneficiary).WithMany(p => p.Deliveries)
                .HasForeignKey(d => d.BeneficiaryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_deliveries_beneficiaries");

            entity.HasOne(d => d.Status).WithMany(p => p.Deliveries)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_deliveries_delivery_status");

            entity.HasOne(d => d.User).WithMany(p => p.Deliveries)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_deliveries_users");
        });

        modelBuilder.Entity<DeliveryItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("delivery_items");

            entity.HasIndex(e => e.DeliveryId, "delivery_items_fk1");

            entity.HasIndex(e => e.ProductLotId, "delivery_items_fk2");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.DeliveryId)
                .HasColumnType("int(11)")
                .HasColumnName("delivery_id");
            entity.Property(e => e.ProductLotId)
                .HasColumnType("int(11)")
                .HasColumnName("product_lot_id");
            entity.Property(e => e.Quantity)
                .HasColumnType("int(11)")
                .HasColumnName("quantity");

            entity.HasOne(d => d.Delivery).WithMany(p => p.DeliveryItems)
                .HasForeignKey(d => d.DeliveryId)
                .HasConstraintName("delivery_items_fk1");

            entity.HasOne(d => d.ProductLot).WithMany(p => p.DeliveryItems)
                .HasForeignKey(d => d.ProductLotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("delivery_items_fk2");
        });

        modelBuilder.Entity<DeliveryStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("delivery_status");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Status)
                .HasMaxLength(255)
                .HasColumnName("status");
        });

        modelBuilder.Entity<Movement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("movements");

            entity.HasIndex(e => e.UserId, "movements_fk1");

            entity.HasIndex(e => e.MovementTypeId, "movements_fk2");

            entity.HasIndex(e => e.DeliveryId, "movements_fk3");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DeliveryId)
                .HasColumnType("int(11)")
                .HasColumnName("delivery_id");
            entity.Property(e => e.MovementTypeId)
                .HasColumnType("int(11)")
                .HasColumnName("movement_type_id");
            entity.Property(e => e.Note)
                .HasColumnType("text")
                .HasColumnName("note");
            entity.Property(e => e.UserId)
                .HasColumnType("int(11)")
                .HasColumnName("user_id");

            entity.HasOne(d => d.Delivery).WithMany(p => p.Movements)
                .HasForeignKey(d => d.DeliveryId)
                .HasConstraintName("movements_fk3");

            entity.HasOne(d => d.MovementType).WithMany(p => p.Movements)
                .HasForeignKey(d => d.MovementTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("movements_fk2");

            entity.HasOne(d => d.User).WithMany(p => p.Movements)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("movements_fk1");
        });

        modelBuilder.Entity<MovementItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("movement_items");

            entity.HasIndex(e => e.MovementId, "FK_mov_id");

            entity.HasIndex(e => e.ProductLotId, "FK_prod_lot_id");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.MovementId)
                .HasColumnType("int(11)")
                .HasColumnName("movement_id");
            entity.Property(e => e.ProductLotId)
                .HasColumnType("int(11)")
                .HasColumnName("product_lot_id");
            entity.Property(e => e.Quantity)
                .HasColumnType("int(11)")
                .HasColumnName("quantity");

            entity.HasOne(d => d.Movement).WithMany(p => p.MovementItems)
                .HasForeignKey(d => d.MovementId)
                .HasConstraintName("FK_mov_id");

            entity.HasOne(d => d.ProductLot).WithMany(p => p.MovementItems)
                .HasForeignKey(d => d.ProductLotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_prod_lot_id");
        });

        modelBuilder.Entity<MovementType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("movement_types");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Type)
                .HasMaxLength(255)
                .HasColumnName("type");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("notifications");

            entity.HasIndex(e => e.UserId, "FK_notifications_users");

            entity.HasIndex(e => e.StatusId, "notifications_ibfk_1");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasComment("Create Time")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Message)
                .HasMaxLength(255)
                .HasColumnName("message");
            entity.Property(e => e.StatusId)
                .HasColumnType("int(11)")
                .HasColumnName("status_id");
            entity.Property(e => e.UserId)
                .HasComment("User that the notification is for")
                .HasColumnType("int(11)")
                .HasColumnName("user_id");

            entity.HasOne(d => d.Status).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("notifications_ibfk_1");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_notifications_users");
        });

        modelBuilder.Entity<NotificationStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("notification_status");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Status)
                .HasMaxLength(100)
                .HasColumnName("status");
        });

        modelBuilder.Entity<ParticularOb>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.BeneficiaryId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("particular_obs");

            entity.HasIndex(e => e.BeneficiaryId, "FK_particular_obs_beneficiaries");

            entity.Property(e => e.UserId)
                .HasColumnType("int(11)")
                .HasColumnName("user_id");
            entity.Property(e => e.BeneficiaryId)
                .HasColumnType("int(11)")
                .HasColumnName("beneficiary_id");
            entity.Property(e => e.Obs)
                .HasColumnType("text")
                .HasColumnName("obs");

            entity.HasOne(d => d.Beneficiary).WithMany(p => p.ParticularObs)
                .HasForeignKey(d => d.BeneficiaryId)
                .HasConstraintName("FK_particular_obs_beneficiaries");

            entity.HasOne(d => d.User).WithMany(p => p.ParticularObs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Userid");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Barcode).HasName("PRIMARY");

            entity.ToTable("products");

            entity.HasIndex(e => e.UnitId, "FK_products_unit_types");

            entity.HasIndex(e => e.CategoryId, "product_category_fk");

            entity.Property(e => e.Barcode).HasColumnName("barcode");
            entity.Property(e => e.CategoryId)
                .HasColumnType("int(11)")
                .HasColumnName("category_id");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.UnitId)
                .HasColumnType("int(11)")
                .HasColumnName("unit_id");
            entity.Property(e => e.UnitSize)
                .HasColumnType("int(11)")
                .HasColumnName("unit_size");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("product_category_fk");

            entity.HasOne(d => d.Unit).WithMany(p => p.Products)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_products_unit_types");
        });

        modelBuilder.Entity<ProductLot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("product_lot");

            entity.HasIndex(e => new { e.Barcode, e.Lot }, "barcode").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Barcode).HasColumnName("barcode");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.Lot).HasColumnName("lot");
            entity.Property(e => e.Quantity)
                .HasColumnType("int(11)")
                .HasColumnName("quantity");

            entity.HasOne(d => d.BarcodeNavigation).WithMany(p => p.ProductLots)
                .HasForeignKey(d => d.Barcode)
                .HasConstraintName("FK_barcode");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("reports");

            entity.HasIndex(e => e.ReportType, "FK_reports_report_types");

            entity.HasIndex(e => e.CreatorId, "FK_reports_users");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatorId)
                .HasColumnType("int(11)")
                .HasColumnName("creator_id");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
            entity.Property(e => e.ReportType)
                .HasColumnType("int(11)")
                .HasColumnName("report_type");

            entity.HasOne(d => d.Creator).WithMany(p => p.Reports)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_reports_users");

            entity.HasOne(d => d.ReportTypeNavigation).WithMany(p => p.Reports)
                .HasForeignKey(d => d.ReportType)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_reports_report_types");
        });

        modelBuilder.Entity<ReportType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("report_types");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
        });

        modelBuilder.Entity<TokenResetPassword>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");

            entity.ToTable("token_reset_password");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnType("int(11)")
                .HasColumnName("user_id");
            entity.Property(e => e.ExpDate)
                .HasColumnType("timestamp")
                .HasColumnName("exp_date");
            entity.Property(e => e.Token)
                .HasMaxLength(75)
                .HasDefaultValueSql("''")
                .HasColumnName("token");

            entity.HasOne(d => d.User).WithOne(p => p.TokenResetPassword)
                .HasForeignKey<TokenResetPassword>(d => d.UserId)
                .HasConstraintName("FK_UserPwdToken");
        });

        modelBuilder.Entity<UnitType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("unit_types");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.Contact, "contact").IsUnique();

            entity.HasIndex(e => e.Email, "email").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Contact).HasColumnName("contact");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.RefreshToken)
                .HasMaxLength(255)
                .HasColumnName("refresh_token");
            entity.Property(e => e.RefreshTokenExp)
                .HasColumnType("datetime")
                .HasColumnName("refresh_token_exp");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<VDeliveriesDetail>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_deliveries_details");

            entity.Property(e => e.BeneficiaryId).HasColumnType("int(11)");
            entity.Property(e => e.BeneficiaryName).HasMaxLength(50);
            entity.Property(e => e.DeliveryId).HasColumnType("int(11)");
            entity.Property(e => e.DeliveryItemId).HasColumnType("int(11)");
            entity.Property(e => e.ItemCreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("timestamp");
            entity.Property(e => e.ItemQuantity).HasColumnType("int(11)");
            entity.Property(e => e.Note).HasColumnType("text");
            entity.Property(e => e.ProductBarcode).HasMaxLength(255);
            entity.Property(e => e.ProductLotId).HasColumnType("int(11)");
            entity.Property(e => e.ProductLotNumber).HasMaxLength(255);
            entity.Property(e => e.ProductName).HasMaxLength(255);
            entity.Property(e => e.StatusId).HasColumnType("int(11)");
            entity.Property(e => e.UserId).HasColumnType("int(11)");
            entity.Property(e => e.UserName).HasMaxLength(255);
        });

        modelBuilder.Entity<VDelivery>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_deliveries");

            entity.Property(e => e.BeneficiaryId).HasColumnType("int(11)");
            entity.Property(e => e.BeneficiaryName).HasMaxLength(50);
            entity.Property(e => e.DeliveryId).HasColumnType("int(11)");
            entity.Property(e => e.Note).HasColumnType("text");
            entity.Property(e => e.StatusId).HasColumnType("int(11)");
            entity.Property(e => e.UserId).HasColumnType("int(11)");
            entity.Property(e => e.UserName).HasMaxLength(255);
        });

        modelBuilder.Entity<VMovHistory>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_mov_history");

            entity.Property(e => e.DeliveryId).HasColumnType("int(11)");
            entity.Property(e => e.MovementDate)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime");
            entity.Property(e => e.MovementId).HasColumnType("int(11)");
            entity.Property(e => e.MovementNote).HasColumnType("text");
            entity.Property(e => e.MovementTypeId).HasColumnType("int(11)");
            entity.Property(e => e.TotalQuantityAffected).HasPrecision(32);
            entity.Property(e => e.UserId).HasColumnType("int(11)");
            entity.Property(e => e.UserName).HasMaxLength(255);
        });

        modelBuilder.Entity<VMovHistoryDetail>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_mov_history_details");

            entity.Property(e => e.DeliveryId).HasColumnType("int(11)");
            entity.Property(e => e.ItemQuantityAffected).HasColumnType("int(11)");
            entity.Property(e => e.MovementDate)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime");
            entity.Property(e => e.MovementId).HasColumnType("int(11)");
            entity.Property(e => e.MovementItemId).HasColumnType("int(11)");
            entity.Property(e => e.MovementNote).HasColumnType("text");
            entity.Property(e => e.MovementTypeId).HasColumnType("int(11)");
            entity.Property(e => e.ProductBarcode).HasMaxLength(255);
            entity.Property(e => e.ProductLotId).HasColumnType("int(11)");
            entity.Property(e => e.ProductLotNumber).HasMaxLength(255);
            entity.Property(e => e.ProductName).HasMaxLength(255);
            entity.Property(e => e.UserId).HasColumnType("int(11)");
            entity.Property(e => e.UserName).HasMaxLength(255);
        });

        modelBuilder.Entity<VStockPerLot>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_stock_per_lot");

            entity.Property(e => e.AvailableStock).HasPrecision(33);
            entity.Property(e => e.Barcode).HasMaxLength(255);
            entity.Property(e => e.CategoryId).HasColumnType("int(11)");
            entity.Property(e => e.Lot).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.ProductLotId).HasColumnType("int(11)");
            entity.Property(e => e.ReservedQuantity).HasPrecision(32);
            entity.Property(e => e.TotalQuantity).HasColumnType("int(11)");
            entity.Property(e => e.UnitId).HasColumnType("int(11)");
            entity.Property(e => e.UnitSize).HasColumnType("int(11)");
        });

        modelBuilder.Entity<VStockPerProduct>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_stock_per_product");

            entity.Property(e => e.AvailableStock).HasPrecision(55);
            entity.Property(e => e.Barcode).HasMaxLength(255);
            entity.Property(e => e.CategoryId).HasColumnType("int(11)");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.ReservedQuantity).HasPrecision(54);
            entity.Property(e => e.TotalQuantity).HasPrecision(32);
            entity.Property(e => e.UnitId).HasColumnType("int(11)");
            entity.Property(e => e.UnitSize).HasColumnType("int(11)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
