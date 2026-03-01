using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence.EntityConfigurations;

public class MembershipApplicationRequestConfiguration : IEntityTypeConfiguration<MembershipApplicationRequest> {
    public void Configure(EntityTypeBuilder<MembershipApplicationRequest> builder) {
        builder.HasKey( request => request.Id );
        
        builder.OwnsOne(m => m.Address);
    }
}