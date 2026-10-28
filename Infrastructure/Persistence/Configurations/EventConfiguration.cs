using Application.Identity;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(ev => ev.Id);

        builder.Property(ev => ev.Title).IsRequired().HasMaxLength(200);

        builder.Property(ev => ev.Description).HasMaxLength(2000);

        builder.Property(ev => ev.Location).IsRequired().HasMaxLength(200);

        builder.Property(ev => ev.Price).HasPrecision(18, 2);

        builder.HasOne(ev => ev.Category)
            .WithMany(category => category.Events)
            .HasForeignKey(ev => ev.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(ev => ev.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
