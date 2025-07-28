using DocuFlow.Domain.Entities;

namespace DocuFlow.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
}
