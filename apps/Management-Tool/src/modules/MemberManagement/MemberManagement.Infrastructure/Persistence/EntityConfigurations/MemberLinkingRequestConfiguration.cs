using MemberManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemberManagement.Infrastructure.Persistence.EntityConfigurations;

public class MemberLinkingRequestConfiguration : IEntityTypeConfiguration<MemberLinkingRequest> {
    public void Configure(EntityTypeBuilder<MemberLinkingRequest> builder) {
        builder.HasKey( request => request.Id );
    }
}