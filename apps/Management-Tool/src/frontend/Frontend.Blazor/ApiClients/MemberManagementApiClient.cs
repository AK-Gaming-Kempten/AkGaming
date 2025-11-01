using AKG.Common.Generics;
using MemberManagement.Contracts.DTO;
using MemberManagement.Contracts.Enums;

namespace Frontend.Blazor.ApiClients;

public class MemberManagementApiClient
{
    private readonly HttpClient _httpClient;

    public MemberManagementApiClient(HttpClient http)
    {
        _httpClient = http;
    }
    
    public async Task<Result<string>> TestAuth() {
        var httpResult = await _httpClient.GetAsync("/test-auth");
        if(!httpResult.IsSuccessStatusCode)
            return Result<string>.Failure(httpResult.ReasonPhrase);
        
        return Result<string>.Success(await httpResult.Content.ReadAsStringAsync());
    }
    
    public async Task<Result<MemberDto>> GetMemberByGuidAsync(Guid id) {
        var httpResult = await _httpClient.GetAsync($"members/{id}");
        if(!httpResult.IsSuccessStatusCode)
            return Result<MemberDto>.Failure(httpResult.ReasonPhrase);
        
        var dto = await httpResult.Content.ReadFromJsonAsync<MemberDto>();
        if( dto == null)
            return Result<MemberDto>.Failure("Unable to read member from response");

        return Result<MemberDto>.Success(dto);
    }
    
    public async Task<Result<MemberDto>> GetMemberByUserGuidAsync(Guid id) {
        var httpResult = await _httpClient.GetAsync($"members/user/{id}");
        if(!httpResult.IsSuccessStatusCode)
            return Result<MemberDto>.Failure(httpResult.ReasonPhrase);
        
        var dto = await httpResult.Content.ReadFromJsonAsync<MemberDto>();
        if( dto == null)
            return Result<MemberDto>.Failure("Unable to read member from response");

        return Result<MemberDto>.Success(dto);
    }
    
    public async Task<Result<ICollection<MemberDto>>> GetAllMembersAsync() {
        var httpResult = await _httpClient.GetAsync("members");
        if(!httpResult.IsSuccessStatusCode)
            return Result<ICollection<MemberDto>>.Failure(httpResult.ReasonPhrase);
        
        var dtos = await httpResult.Content.ReadFromJsonAsync<List<MemberDto>>();
        if( dtos == null)
            return Result<ICollection<MemberDto>>.Failure("Unable to read members from response");

        return Result<ICollection<MemberDto>>.Success(dtos);
    }
    
    public async Task<Result<ICollection<MemberDto>>> GetMembersWithStatusAsync(MembershipStatus status) {
        var httpResult = await _httpClient.GetAsync($"members?status={status}");
        if(!httpResult.IsSuccessStatusCode)
            return Result<ICollection<MemberDto>>.Failure(httpResult.ReasonPhrase);
        
        var dtos = await httpResult.Content.ReadFromJsonAsync<List<MemberDto>>();
        if( dtos == null)
            return Result<ICollection<MemberDto>>.Failure("Unable to read members from response");

        return Result<ICollection<MemberDto>>.Success(dtos);
    }
    
    public async Task<Result<ICollection<MemberDto>>> GetMembersWithStatusAsync(ICollection<MembershipStatus> statuses) {
        var httpResult = await _httpClient.GetAsync($"members?status={string.Join(",", statuses)}");
        if(!httpResult.IsSuccessStatusCode)
            return Result<ICollection<MemberDto>>.Failure(httpResult.ReasonPhrase);
        
        var dtos = await httpResult.Content.ReadFromJsonAsync<List<MemberDto>>();
        if( dtos == null)
            return Result<ICollection<MemberDto>>.Failure("Unable to read members from response");

        return Result<ICollection<MemberDto>>.Success(dtos);
    }
    
    public async Task<Result> CreateMemberAsync(MemberCreationDto dto) {
        var httpResult = await _httpClient.PostAsJsonAsync("members", dto);
        return httpResult.IsSuccessStatusCode ? Result.Success() : Result<MemberDto>.Failure(httpResult.ReasonPhrase);
    }
    
    public async Task<Result> UpdateMemberAsync(MemberDto dto) {
        var httpResult = await _httpClient.PutAsJsonAsync($"members/{dto.Id}", dto);
        return httpResult.IsSuccessStatusCode ? Result.Success() : Result<MemberDto>.Failure(httpResult.ReasonPhrase);
    }
}