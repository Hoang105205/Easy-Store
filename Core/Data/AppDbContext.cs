using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Core.Data;

public class AppDbContext : DbContext
{
    // 1. CONSTRUCTOR: "Cái phễu" nhận cấu hình
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // 2. KHAI BÁO CÁC BẢNG (DbSet)
    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<ImportLog> ImportLogs { get; set; }
    public DbSet<ImportLogDetail> ImportLogDetails { get; set; }

    // 3. FLUENT API: Thiết lập luật lệ ngầm cho PostgreSQL
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- CẤU HÌNH TỰ ĐỘNG SINH UUID CHO KHÓA CHÍNH ---
        // PostgreSQL có hàm gen_random_uuid() cực nhanh, ta ép EF Core gọi hàm này
        // mỗi khi có một dòng dữ liệu mới được thêm vào.

        modelBuilder.Entity<User>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        modelBuilder.Entity<Product>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        modelBuilder.Entity<ProductImage>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        modelBuilder.Entity<Category>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        modelBuilder.Entity<Order>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        modelBuilder.Entity<OrderItem>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        modelBuilder.Entity<ImportLog>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        modelBuilder.Entity<ImportLogDetail>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        // --- CẤU HÌNH RIÊNG CHO ĐƠN HÀNG ---
        // Mã hóa đơn (ReceiptNumber) không dùng UUID cho khách hàng dễ đọc. 
        // Ta cấu hình nó thành cột SERIAL (tự động tăng: 1, 2, 3...)
        modelBuilder.Entity<Order>()
            .Property(o => o.ReceiptNumber)
            .UseSerialColumn();
    }
}
